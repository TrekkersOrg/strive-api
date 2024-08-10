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
        public int? riskAssessmentScore { get; set; }
        public int? financialScore { get; set; }
        public int? financialSystemQueryScore { get; set; }
        public int? financialKeywordsScore { get; set; }
        public int? financialXgbScore { get; set; }
        public int? reputationalScore { get; set; }
        public int? reputationalSystemQueryScore { get; set; }
        public int? reputationalKeywordsScore { get; set; }
        public int? reputationalXgbScore { get; set; }
        public int? regulatoryScore { get; set; }
        public int? regulatorySystemQueryScore { get; set; }
        public int? regulatoryKeywordsScore { get; set; }
        public int? regulatoryXgbScore { get; set; }
        public int? operationalScore { get; set; }
        public int? operationalSystemQueryScore { get; set; }
        public int? operationalKeywordsScore { get; set; }
        public int? operationalXgbScore { get; set; }
    }
}
