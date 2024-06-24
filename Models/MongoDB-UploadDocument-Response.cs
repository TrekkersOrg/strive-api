/// <summary>
/// Represents the response body of the /mongodb/uploaddocument endpoint.
/// </summary>
namespace strive_api.Models
{
    public class MongoDB_UploadDocument_Response
    {
        public string? CollectionName { get; set; }
        public string? FileName { get; set; }
    }
}
