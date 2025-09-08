using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

namespace GSADUs.Revit.Addin
{
    [Transaction(TransactionMode.Manual)]
    public class BatchExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string msg, ElementSet set)
        {
            var uiapp = data.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) { TaskDialog.Show("Batch Export", "Open a document."); return Result.Cancelled; }
            var doc = uidoc.Document;

            if (string.IsNullOrWhiteSpace(doc.PathName))
            { TaskDialog.Show("Batch Export", "Save the model first."); return Result.Cancelled; }

            var allSets = SelectionSets.Get(doc);
            if (allSets.Count == 0)
            { TaskDialog.Show("Batch Export", "No Selection Filters found."); return Result.Cancelled; }

            // UI collects Overwrite, SaveBefore, RecenterXY. (present in XAML/Window) 
            var win = new UI.BatchExportWindow(allSets.Select(s => s.Name));

            // preload last output folder if available
            var last = UserPrefs.LoadLastOutputDir();
            if (!string.IsNullOrWhiteSpace(last))
            {
                var tb = win.FindName("OutputPath") as System.Windows.Controls.TextBox
                      ?? win.FindName("OutputDir") as System.Windows.Controls.TextBox
                      ?? win.FindName("Output") as System.Windows.Controls.TextBox;
                if (tb != null) tb.Text = last;
            }

            if (win.ShowDialog() != true) return Result.Cancelled;
            var s = win.Result!;
            if (s.SaveBefore && doc.IsModified) doc.Save();
            UserPrefs.SaveLastOutputDir(s.OutputDir);

            // Capture UniqueIds for each set so we can resolve the same elements in the copied file.
            var uniqueBySet = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, ids) in allSets)
            {
                var list = new List<string>(ids.Count);
                foreach (var id in ids)
                {
                    var e = doc.GetElement(id);
                    if (e != null && !string.IsNullOrEmpty(e.UniqueId)) list.Add(e.UniqueId);
                }
                uniqueBySet[name] = list;
            }

            foreach (var setName in s.SetNames)
            {
                try
                {
                    var outFile = Path.Combine(s.OutputDir, $"{San(setName)}.rvt");

                    if (!s.Overwrite && File.Exists(outFile))
                        throw new IOException("File exists and Overwrite is off.");

                    // 1) Write copy
                    File.Copy(doc.PathName, outFile, s.Overwrite);
                    if (s.RecenterXY && uniqueBySet.TryGetValue(setName, out var uids) && uids.Count > 0)
                    {
                        var outDoc = uiapp.Application.OpenDocumentFile(outFile);
                        try
                        {
                            // Resolve element Ids in the copied file
                            var ids = new System.Collections.Generic.List<Autodesk.Revit.DB.ElementId>(uids.Count);
                            foreach (var uid in uids)
                            {
                                var e = outDoc.GetElement(uid);
                                if (e != null) ids.Add(e.Id);
                            }
                            if (ids.Count == 0) { outDoc.Save(); continue; }
                            // GSADUs: center saved set to Internal Origin in XY once
                            if (s.RecenterXY)
                            {
                                RecenterUtil.CenterSelectionXY(outDoc, ids);
                            }

                            // One AABB from Walls across the whole set
                            double minx = double.PositiveInfinity, miny = double.PositiveInfinity;
                            double maxx = double.NegativeInfinity, maxy = double.NegativeInfinity;
                            bool any = false;

                            foreach (var id in ids)
                            {
                                var e = outDoc.GetElement(id);
                                // Include only Walls; if none contribute we skip the move
                                if (e?.Category == null || e.Category.Id != new ElementId((int)Autodesk.Revit.DB.BuiltInCategory.OST_Walls))
                                    continue;

                                Autodesk.Revit.DB.BoundingBoxXYZ? bb = null;
                                try { bb = e.get_BoundingBox(null); } catch { bb = null; }
                                if (bb == null) continue;

                                minx = System.Math.Min(minx, System.Math.Min(bb.Min.X, bb.Max.X));
                                miny = System.Math.Min(miny, System.Math.Min(bb.Min.Y, bb.Max.Y));
                                maxx = System.Math.Max(maxx, System.Math.Max(bb.Min.X, bb.Max.X));
                                maxy = System.Math.Max(maxy, System.Math.Max(bb.Min.Y, bb.Max.Y));
                                any = true;
                            }

                            if (any)
                            {
                                var cx = 0.5 * (minx + maxx);
                                var cy = 0.5 * (miny + maxy);
                                var delta = new Autodesk.Revit.DB.XYZ(-cx, -cy, 0.0);

                                using (var t = new Autodesk.Revit.DB.Transaction(outDoc, "GSADUs: Center To Origin"))
                                {
                                    t.Start();
                                    try
                                    {
                                        Autodesk.Revit.DB.ElementTransformUtils.MoveElements(outDoc, ids, delta);
                                        t.Commit();
                                    }
                                    catch { t.RollBack(); throw; }
                                }
                            }

                            outDoc.Save();
                        }
                        finally
                        {
                            try { outDoc.Close(false); } catch { }
                        }
                    }

                    // 2) Center to origin (XY) for this set only, single BB, single move
                    if (s.RecenterXY && uniqueBySet.TryGetValue(setName, out var uidsForSet) && uidsForSet.Count > 0)
                    {
                        var outDoc = uiapp.Application.OpenDocumentFile(outFile);
                        try
                        {
                            var ids = new List<ElementId>(uidsForSet.Count);
                            foreach (var uid in uidsForSet)
                            {
                                var el = outDoc.GetElement(uid);
                                if (el != null) ids.Add(el.Id);
                            }
                            if (ids.Count == 0) { outDoc.Save(); continue; }

                            RecenterUtil.CenterSelectionXY(outDoc, ids); // call once
                            outDoc.Save();
                        }
                        finally { try { outDoc.Close(false); } catch { } }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Batch Export", $"Set '{setName}' failed:\n{ex.Message}");
                }
            }

            TaskDialog.Show("Batch Export", "Done.");
            return Result.Succeeded;
        }

        static string San(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }
    }

    internal static class UserPrefs
    {
        static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GSADUs", "Revit", "Addin");
        static readonly string FilePath = Path.Combine(Dir, "last_output_dir.txt");

        public static string LoadLastOutputDir()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var p = File.ReadAllText(FilePath).Trim();
                    return Directory.Exists(p) ? p : "";
                }
            }
            catch { }
            return "";
        }

        public static void SaveLastOutputDir(string path)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(FilePath, path ?? "");
            }
            catch { }
        }
    }
}







