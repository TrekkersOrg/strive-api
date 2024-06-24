/// <summary>
/// Represents the response body of the /mongodb/getdocument endpoint.
/// </summary>
namespace strive_api.Models
{
    public class MongoDB_GetDocument_Response
    {
        public bool? FileExists { get; set; }
        public string? FileName { get; set; }
        public string? CollectionName { get; set; }
    }
}
