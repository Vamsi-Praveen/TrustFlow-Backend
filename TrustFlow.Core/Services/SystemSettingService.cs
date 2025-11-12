using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
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
        private readonly IMongoCollection<PortalConfig> _portalConfig;

        public SystemSettingService(TeamsService teamsService, SlackService slackService, EmailService emailService, ILogger<SystemSettingService> logger, ApplicationContext context)
        {
            _teamsService = teamsService;
            _slackService = slackService;
            _emailService = emailService;
            _logger = logger;
            _portalConfig = context.PortalConfig;
        }

        public async Task<ServiceResult> GetSystemSettings()
        {
            try
            {
                var smtpConfig = await _emailService.GetConfig();
                var teamsConfig = await _teamsService.GetTeamsDetailsAsync();
                var slackConfig = await _slackService.GetSlackDetailsAsync();
                var portalConfig = await _portalConfig.Find(_ => true).FirstOrDefaultAsync();

                var settings = new SystemSettings()
                {
                    slackConfig = (SlackConfig)slackConfig.Result,
                    smtpConfig = (SMTPConfig)smtpConfig.Result,
                    teamsConfig = (TeamsConfig)teamsConfig.Result,
                    portalConfig = (PortalConfig)portalConfig
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
