namespace GSADUs.Core
{
    public class ExportResult
    {
        public string SetName { get; set; }
        public int ElementCount { get; set; }
        public string BBoxMin { get; set; }
        public string BBoxMax { get; set; }
        public string Transform { get; set; }
        public string OutputPath { get; set; }
        public long ElapsedMs { get; set; }
        public string Warnings { get; set; }
        public string Errors { get; set; }
    }
}