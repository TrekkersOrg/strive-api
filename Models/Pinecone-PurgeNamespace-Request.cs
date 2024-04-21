using System.Text.Json;

namespace strive_api.Models
{
    public class Pinecone_PurgeNamespace_Request
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
