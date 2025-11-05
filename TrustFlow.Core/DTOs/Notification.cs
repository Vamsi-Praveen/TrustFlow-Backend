namespace TrustFlow.Core.DTOs
{
    public class Notification
    {
        public string ProjectName {get; set; }

        public string IssueName {get; set; }

        public string IssueDescription { get; set; }

        public string ReportedUserName { get; set; }

        public string? ReportedUserEmail { get; set; }

        public string AssignedUserName { get; set; }

        public string? AssignedUserEmail { get; set; }
    }
}
