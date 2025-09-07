using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GSADUs.Revit.Addin;
using GSADUs.Revit.Addin.UI;   // <— add this
using System.Linq;

[Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
public class BatchExportCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string msg, ElementSet set)
    {
        var uidoc = data.Application.ActiveUIDocument;
        if (uidoc == null) { TaskDialog.Show("Batch Export", "Open a document."); return Result.Cancelled; }
        var doc = uidoc.Document;

        var allSets = SelectionSets.Get(doc);
        if (allSets.Count == 0) { TaskDialog.Show("Batch Export", "No Selection Filters found."); return Result.Cancelled; }

        var win = new BatchExportWindow(allSets.Select(s => s.Name));
        if (win.ShowDialog() != true) return Result.Cancelled;

        var s = win.Result!; // dialog guarantees non-null on OK
        TaskDialog.Show("Batch Export", $"Sets: {string.Join(", ", s.SetNames)}\nOut: {s.OutputDir}");
        return Result.Succeeded;
    }
}

