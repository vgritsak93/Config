namespace GSADUsRevitAddin.Batch
{
    public class BatchProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static BatchProcessResult Ok(string message)
        {
            return new BatchProcessResult { Success = true, Message = message };
        }

        public static BatchProcessResult Fail(string message)
        {
            return new BatchProcessResult { Success = false, Message = message };
        }
    }
}