using System;
using System.IO;
using System.Linq;
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

      var win = new UI.BatchExportWindow(allSets.Select(s => s.Name));
      if (win.ShowDialog() != true) return Result.Cancelled;
      var s = win.Result!;
      if (s.SaveBefore && doc.IsModified) doc.Save();

      var modelName = Path.GetFileNameWithoutExtension(doc.PathName);

      foreach (var setName in s.SetNames)
      {
        try
        {
          var outFile = Path.Combine(s.OutputDir,
            $"{San(modelName)}__{San(setName)}.rvt");

          if (!s.Overwrite && File.Exists(outFile))
            throw new IOException("File exists and Overwrite is off.");

          File.Copy(doc.PathName, outFile, s.Overwrite);
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
}
