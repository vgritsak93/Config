// Batch/OpenAndSaveProcess.cs
using System;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GSADUsRevitAddin.Batch
{
    /// <summary>
    /// Opens a model and saves it if modified. No edits are performed.
    /// Useful as a baseline or to upgrade and resave files opened by newer Revit.
    /// </summary>
    public sealed class OpenAndSaveProcess : IBatchProcess
    {
        public string Id => "OpenAndSave";
        public string Name => "Open and Save";
        public string Description => "Opens and saves the document.";
        public bool MightModifyDocument => true;

        public ProcessResult Execute(UIApplication uiapp, Document doc, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (doc != null && doc.IsModified)
                {
                    SaveOptions so = new SaveOptions { Compact = true };
                    doc.Save(so);
                    return new ProcessResult { Success = true, Message = "Document saved." };
                }

                return new ProcessResult { Success = true, Message = "No changes to save." };
            }
            catch (OperationCanceledException)
            {
                return new ProcessResult { Success = false, Message = "Canceled." };
            }
            catch (Exception ex)
            {
                return new ProcessResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}
