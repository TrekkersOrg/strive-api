using System.Text.Json;

/// <summary>
/// Represents the request body of the /pinecone/purgePinecone endpoint.
/// </summary>
namespace strive_api.Models
{
    public class Pinecone_PurgePinecone_Request
    {
        public string? Namespace { get; set; }

        public bool DeleteAll { get; set; } = true;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
    }
}
