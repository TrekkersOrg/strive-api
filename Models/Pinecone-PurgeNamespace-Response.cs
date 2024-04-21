using System.Text.Json;

namespace strive_api.Models
{
    public class Pinecone_PurgeNamespace_Response
    {
        public int NumberOfVectorsDeleted { get; set; }
        public string? Namespace { get; set; }
    }
}
