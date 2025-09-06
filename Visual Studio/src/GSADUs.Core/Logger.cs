using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GSADUs.Core
{
    public static class Logger
    {
        public static void LogResults(string csvPath, IEnumerable<ExportResult> results)
        {
            var header = "SetName,ElementCount,BBoxMin,BBoxMax,Transform,OutputPath,ElapsedMs,Warnings,Errors";
            var lines = results
                .OrderBy(r => r.SetName)
                .Select(r => $"{r.SetName},{r.ElementCount},{r.BBoxMin},{r.BBoxMax},{r.Transform},{r.OutputPath},{r.ElapsedMs},{r.Warnings},{r.Errors}");

            File.WriteAllLines(csvPath, new[] { header }.Concat(lines), Encoding.UTF8);
        }
    }
}