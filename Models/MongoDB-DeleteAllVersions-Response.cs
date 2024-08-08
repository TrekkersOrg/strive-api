namespace strive_api.Models
{
    public class MongoDB_DeleteAllVersions_Response
    {
        public string? FileName { get; set; }
        public string? Namespace { get; set; }
        public long NumberOfDocuments { get; set; }
    }
}