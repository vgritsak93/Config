using System;
using System.IO;
using System.Linq;
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

      var win = new UI.BatchExportWindow(allSets.Select(s => s.Name));

      // preload last output folder if available
      var last = UserPrefs.LoadLastOutputDir();
      if (!string.IsNullOrWhiteSpace(last))
      {
        var tb = win.FindName("OutputPath") as System.Windows.Controls.TextBox
              ?? win.FindName("OutputDir")  as System.Windows.Controls.TextBox
              ?? win.FindName("Output")     as System.Windows.Controls.TextBox;
        if (tb != null) tb.Text = last;
      }

      if (win.ShowDialog() != true) return Result.Cancelled;
      var s = win.Result!;
      if (s.SaveBefore && doc.IsModified) doc.Save();
      UserPrefs.SaveLastOutputDir(s.OutputDir);

      foreach (var setName in s.SetNames)
      {
        try
        {
          var outFile = Path.Combine(s.OutputDir, $"{San(setName)}.rvt");

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

  internal static class UserPrefs
  {
    static readonly string Dir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GSADUs","Revit","Addin");
    static readonly string FilePath = Path.Combine(Dir, "last_output_dir.txt");

    public static string LoadLastOutputDir()
    {
      try {
        if (File.Exists(FilePath))
        {
          var p = File.ReadAllText(FilePath).Trim();
          return Directory.Exists(p) ? p : "";
        }
      } catch {}
      return "";
    }

    public static void SaveLastOutputDir(string path)
    {
      try {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, path ?? "");
      } catch {}
    }
  }
}



