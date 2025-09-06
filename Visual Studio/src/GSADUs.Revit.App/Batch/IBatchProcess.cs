// Batch/IBatchProcess.cs
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GSADUsRevitAddin.Batch
{
    public interface IBatchProcess
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        /// <summary>True if this process may modify the document.</summary>
        bool MightModifyDocument { get; }

        ProcessResult Execute(UIApplication uiapp, Document doc, CancellationToken token);
    }

    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
