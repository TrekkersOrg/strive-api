using System.Text.Json;

/// <summary>
/// Represents the response body of the /pinecone/purgePinecone endpoint.
/// </summary>
namespace strive_api.Models
{
    public class Pinecone_PurgePinecone_Response
    {
        public int NumberOfVectorsDeleted { get; set; }
        public string? Namespace { get; set; }
    }
}
