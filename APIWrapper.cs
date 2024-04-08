namespace strive_api
{
    public class APIWrapper
    {
        public int StatusCode { get; set; }
        public string? StatusMessage { get; set; }
        public string? StatusMessageText { get; set; }
        public DateTime Timestamp { get; set; }
        public Object? Data { get; set; }
    }
}
