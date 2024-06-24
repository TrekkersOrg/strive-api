/// <summary>
/// Represents the response body of the /email/sendEmail endpoint.
/// </summary>
namespace strive_api.Models
{
    public class Email_SendEmail_Response
    {
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
        public string? ToEmail { get; set; }
        public string? ToName { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
