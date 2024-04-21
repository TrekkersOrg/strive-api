namespace strive_api.Models
{
    public class Pinecone_IndexDetails_Response
    {
        public Dictionary<string, NamespaceModel>? Namespaces { get; set; }
        public int Dimension { get; set; }
        public double IndexFullness { get; set; }
        public int TotalVectorCount { get; set; }
        public class NamespaceModel
        {
            public int VectorCount { get; set; }
        }
    }
}
