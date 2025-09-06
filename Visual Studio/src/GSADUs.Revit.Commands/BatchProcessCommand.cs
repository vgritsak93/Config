// GSADUs.Revit.Commands/BatchProcessCommand.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GSADUs.Revit.App.Batch;
using GSADUsRevitAddin.Services;            // updated namespace for SettingsService
using GSADUs.Revit.Commands.Forms;     // BatchProcessPickerForm

namespace GSADUs.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public sealed class BatchProcessCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            UIApplication uiapp = data.Application;

            // 1) Choose processes
            var processes = new List<IBatchProcess>
            {
                new OpenAndSaveProcess(),
                // add more processes here
            };

            using (var dlg = new BatchProcessPickerForm(processes))
            {
                // If you have a Revit-owned IWin32Window wrapper, pass it to ShowDialog(owner).
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return Result.Cancelled;

                // 2) Validate folder
                var folder = dlg.SelectedFolder;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    message = "Folder not found.";
                    return Result.Failed;
                }

                // 3) Persist last-used export root (optional)
                SettingsService.Instance.SetExportRootPath(folder);

                // 4) Run batch
                var files = Directory.EnumerateFiles(folder, "*.rvt", SearchOption.TopDirectoryOnly);
                var runner = new BatchRunner();

                // Simple progress reporter (optional)
                var progress = new Progress<BatchRunner.BatchProgress>(p =>
                    TaskDialog.Show("Batch", $"{p.Index}/{p.Total} {p.Phase}: {p.FilePath}"));

                foreach (var res in runner.Run(uiapp, files, dlg.SelectedProcesses, progress, CancellationToken.None))
                {
                    // You can aggregate results for a final summary or log per-file here.
                    // Example: write to Revit journal or your CSV logger.
                }
            }

            return Result.Succeeded;
        }
    }
}
