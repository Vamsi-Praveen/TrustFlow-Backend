using TrustFlow.Core.Models;

namespace TrustFlow.Core.DTOs
{
    public class SystemSettings
    {
        public SMTPConfig smtpConfig {  get; set; }

        public TeamsConfig teamsConfig { get; set; }

        public SlackConfig slackConfig { get; set; }
    }
}
