// Logging/SelectionSetAuditLogger.cs
using System;
using System.IO;
using System.Text;
using GSADUsRevitAddin.Services;

namespace GSADUsRevitAddin.Logging
{
    public static class SelectionSetAuditLogger
    {
        private static string CsvPath => Path.Combine(SettingsService.Instance.LogsDir, "SelectionSetAuditLog.csv");

        public static void Log(SelectionSetAuditEntry e)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CsvPath)!);
            bool writeHeader = !File.Exists(CsvPath);

            using var sw = new StreamWriter(CsvPath, append: true, Encoding.UTF8);
            if (writeHeader)
            {
                sw.WriteLine("TimestampUtc,DocumentTitle,DocumentPath,SetName,Category,ElementCount,Notes");
            }
            sw.WriteLine($"{e.TimestampUtc:O},{Esc(e.DocumentTitle)},{Esc(e.DocumentPath)},{Esc(e.SetName)},{Esc(e.Category)},{e.ElementCount},{Esc(e.Notes)}");
        }

        private static string Esc(string? s)
        {
            s ??= "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }

    public sealed class SelectionSetAuditEntry
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string DocumentTitle { get; set; } = "";
        public string DocumentPath { get; set; } = "";
        public string SetName { get; set; } = "";
        public string Category { get; set; } = "";
        public int ElementCount { get; set; }
        public string Notes { get; set; } = "";
    }
}
