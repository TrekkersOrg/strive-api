using System.Text.Json;

/// <summary>
/// Represents the request body of the /pinecone/getRecord endpoint.
/// </summary>
namespace strive_api.Models
{
    public class Pinecone_GetRecord_Request
    {
        public List<string>? Ids { get; set; }
        public string? Namespace { get; set; }

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
