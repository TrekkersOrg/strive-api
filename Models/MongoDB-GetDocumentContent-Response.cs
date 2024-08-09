namespace strive_api.Models
{
    public class MongoDB_GetDocumentContent_Response
    {
        public string? FileName {  get; set; }
        public string? Content { get; set; }
        public int Version { get; set; }
    }
}
