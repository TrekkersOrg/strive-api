namespace strive_api.Models
{
    public class Pinecone_GetRecord_Response
    {
        public string? Namespace { get; set; }
        public UsageDetails? Usage { get; set; }
        public VectorsDetails? Vectors { get; set; }

        public class VectorsDetails
        {
            public Dictionary<string, VectorDetails>? Vectors { get; set; }
        }

        public class VectorDetails
        {
            public string? Id { get; set; }
            public List<double>? Values { get; set; }
        }

        public class UsageDetails
        {
            public int ReadUnits { get; set; }
        }
    }
}
