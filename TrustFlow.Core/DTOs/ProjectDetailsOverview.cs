namespace TrustFlow.Core.DTOs
{
    public class ProjectDetailsOverview
    {

        public List<ProjectMembers> Members { get; set; } = new List<ProjectMembers>();

        public ProjectStats ProjectStats { get; set; }
    }


    public class ProjectMembers
    {
        public string MemberName { get; set; }
        public string MemberEmail { get; set; }
        public string? MemberProfileImage { get; set; }

        public int MemberAssignedIssues { get; set; }
    }

    public class ProjectStats
    {
        public int TotalIssues { get; set; }

        public int OpenIssues { get; set; }

        public int Bugs{ get; set; }
    }
}
