namespace TrustFlow.Core.DTOs
{
    public class LookupDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class IssueDto
    {
        public string Id { get; set; }
        public string IssueId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ProjectId { get; set; }

        public LookupDto Status { get; set; }
        public LookupDto Priority { get; set; }
        public LookupDto Type { get; set; }
        public LookupDto Severity { get; set; }

        public LookupDto Reporter { get; set; }
        public List<LookupDto> Assignees { get; set; } = new List<LookupDto>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
