/// <summary>
/// Represents the metadata of all API responses.
/// </summary>
namespace strive_api.Models
{
    public class APIWrapper
    {
        public int StatusCode { get; set; }
        public string? StatusMessage { get; set; }
        public string? StatusMessageText { get; set; }
        public DateTime Timestamp { get; set; }
        public object? Data { get; set; }
    }
}
