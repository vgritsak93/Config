using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GSADUs.Revit.Commands; // If BatchExportForm is in this namespace
// If BatchExportForm is in a different namespace, use that instead

namespace GSADUs.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class BatchExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            // Show WinForms UI
            using var form = new BatchExportForm();
            var result = form.ShowDialog(); // Revit loads WinForms fine
            if (result != DialogResult.OK) return Result.Cancelled;

            var uiApp = data.Application;

            foreach (var rvt in Directory.EnumerateFiles(form.InputFolder, "*.rvt", SearchOption.TopDirectoryOnly))
            {
                var doc = uiApp.Application.OpenDocumentFile(rvt);
                try
                {
                    // minimal: copy/save-as to output; replace with your real export
                    var target = Path.Combine(form.OutputFolder, Path.GetFileName(rvt));
                    var opts = new SaveAsOptions { Compact = true };
                    doc.SaveAs(target, opts);
                }
                finally
                {
                    doc.Close(false);
                }
            }

            TaskDialog.Show("GSADUs", "Batch export complete.");
            return Result.Succeeded;
        }
    }
}
