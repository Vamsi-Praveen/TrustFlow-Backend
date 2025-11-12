using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustFlow.Core.DTOs
{
    public class ProjectIssueAnalyticsDto
    {
        public string ProjectId { get; set; }
        public int TotalIssues { get; set; }
        public int OpenIssues { get; set; }
        public int ClosedIssues { get; set; }
    }
}
