// Batch/BatchRunner.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GSADUsRevitAddin.Batch
{
    public class BatchRunner
    {
        public class BatchProgress
        {
            public int Index { get; set; }
            public int Total { get; set; }
            public string FilePath { get; set; }
            public string Phase { get; set; }
        }

        public IEnumerable<object> Run(UIApplication uiapp, IEnumerable<string> files, List<IBatchProcess> procList, IProgress<BatchProgress> progress, CancellationToken token)
        {
            int i = 0;
            int total = 0;
            if (files is ICollection<string> col)
                total = col.Count;
            else
                total = new List<string>(files).Count;

            foreach (var path in files)
            {
                Document doc = null;
                try
                {
                    var mp = ModelPathUtils.ConvertUserVisiblePathToModelPath(path);
                    var openOptions = new OpenOptions { DetachFromCentralOption = DetachFromCentralOption.DoNotDetach };
                    doc = uiapp.Application.OpenDocumentFile(mp, openOptions);

                    progress?.Report(new BatchProgress { Index = i + 1, Total = total, FilePath = path, Phase = "process" });

                    foreach (var p in procList)
                    {
                        token.ThrowIfCancellationRequested();
                        var pr = p.Execute(uiapp, doc, token);
                        // You can aggregate results here
                    }

                    if (doc.IsModified)
                    {
                        var so = new SaveOptions { Compact = true };
                        doc.Save(so);
                    }
                }
                finally
                {
                    progress?.Report(new BatchProgress { Index = i + 1, Total = total, FilePath = path, Phase = "close" });
                    if (doc != null && doc.IsValidObject)
                    {
                        try { doc.Close(false); } catch { }
                    }
                }
                i++;
                yield return null;
            }
        }
    }
}
