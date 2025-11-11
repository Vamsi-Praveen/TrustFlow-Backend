using Microsoft.Extensions.Logging;
using TrustFlow.Core.Communication;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class SystemSettingService
    {
        private readonly TeamsService _teamsService;
        private readonly SlackService _slackService;
        private readonly EmailService _emailService;
        private readonly ILogger<SystemSettingService> _logger;

        public SystemSettingService(TeamsService teamsService, SlackService slackService, EmailService emailService, ILogger<SystemSettingService> logger)
        {
            _teamsService = teamsService;
            _slackService = slackService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ServiceResult> GetSystemSettings()
        {
            try
            {
                var smtpConfig = await _emailService.GetConfig();
                var teamsConfig = await _teamsService.GetTeamsDetailsAsync();
                var slackConfig = await _slackService.GetSlackDetailsAsync();

                var settings = new SystemSettings()
                {
                    slackConfig = (SlackConfig)slackConfig.Result,
                    smtpConfig = (SMTPConfig)smtpConfig.Result,
                    teamsConfig = (TeamsConfig)teamsConfig.Result,
                };

                return new ServiceResult(true, "Succesfully fetched the SystemSettings", settings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while Getting System Settings: {ex}");
                return new ServiceResult(false, "An internal error occurred while retrieving system settings.");
            }
        }
    }
}
