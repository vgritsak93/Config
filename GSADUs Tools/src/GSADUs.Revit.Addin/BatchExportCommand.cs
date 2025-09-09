using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
                    FileCleanup.DeleteRvtBackups(Path.GetDirectoryName(outFile));

                    // 2) Recenter if requested
                    if (s.RecenterXY && uniqueBySet.TryGetValue(setName, out var uids) && uids.Count > 0)
                    {
                        var outDoc = uiapp.Application.OpenDocumentFile(outFile);
                        try
                        {
                            var ids = new List<ElementId>(uids.Count);
                            foreach (var uid in uids)
                            {
                                var el = outDoc.GetElement(uid);
                                if (el != null) ids.Add(el.Id);
                            }

                            if (ids.Count > 0)
                            {
                                RecenterUtil.CenterSelectionXY(outDoc, ids);
                                outDoc.Save();
                            }
                        }
                        finally
                        {
                            try { outDoc.Close(false); } catch { }
                        }
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
        static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GSADUs", "Revit", "Addin");

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
