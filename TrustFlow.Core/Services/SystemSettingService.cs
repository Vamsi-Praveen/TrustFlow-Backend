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
        private readonly IMongoCollection<IssueStatus> _issueStatuses;
        private readonly IMongoCollection<IssuePriority> _issuePriority;
        private readonly IMongoCollection<IssueSeverity> _issueSeverity;
        private readonly IMongoCollection<IssueType> _issueType;

        public SystemSettingService(TeamsService teamsService, SlackService slackService, EmailService emailService, ILogger<SystemSettingService> logger, ApplicationContext context)
        {
            _teamsService = teamsService;
            _slackService = slackService;
            _emailService = emailService;
            _logger = logger;
            _portalConfig = context.PortalConfig;
            _issueStatuses = context.IssueStatus;
            _issuePriority = context.IssuePriorities;
            _issueSeverity = context.IssueSeverities;
            _issueType = context.IssueTypes;
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

        public async Task<ServiceResult> UpdatePortalConfig(PortalConfig config)
        {
            try
            {
                var existingPortalConfig = await _portalConfig.Find(_ => true).FirstOrDefaultAsync();

                if (existingPortalConfig == null)
                {
                    _logger.LogWarning("No existing portal configuration found.");
                    return new ServiceResult(false, "Portal configuration not found.");
                }

                var filter = Builders<PortalConfig>.Filter.Eq(x => x.Id, existingPortalConfig.Id);
                var update = Builders<PortalConfig>.Update
                    .Set(x => x.DefaultNotificationMethod, config.DefaultNotificationMethod)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var result = await _portalConfig.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Portal configuration updated successfully.");
                    return new ServiceResult(true, "Portal configuration updated successfully.");
                }

                return new ServiceResult(false, "No changes were made to the portal configuration.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while updating portal config System Settings: {ex}");
                return new ServiceResult(false, "An internal error occurred while updating portal configuration.");
            }
        }

        public async Task<ServiceResult> GetIssueStatuses()
        {
            try
            {
                var issueStatus = await _issueStatuses.Find(_ => true).ToListAsync();

                return new ServiceResult(true, "Succesfully fetched the Issue Status", issueStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while Getting System issue status: {ex}");
                return new ServiceResult(false, "An internal error occurred while retrieving system settings.");
            }
        }

        public async Task<ServiceResult> GetIssueConfigurations()
        {
            try
            {
                var issueStatus = await _issueStatuses.Find(_ => true)
                    .Project(x => new {x.Id, x.Name,x.Description })
                    .ToListAsync();

                var issuePriority = await _issuePriority.Find(_ => true)
                    .Project(x => new { x.Id,x.Name,x.Description })
                    .ToListAsync();

                var issueTypes = await _issueType.Find(_ => true)
                    .Project(x => new { x.Id, x.Name,x.Description })
                    .ToListAsync();

                var issueSeverity = await _issueSeverity.Find(_ => true)
                    .Project(x => new { x.Id, x.Name ,x.Description })
                    .ToListAsync();

                var issueConfigurations = new
                {
                    issueStatus,
                    issuePriority,
                    issueTypes,
                    issueSeverity
                };

                return new ServiceResult(true, "Succesfully fetched the Issue Configurations", issueConfigurations);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while Getting System issue configuration: {ex}");
                return new ServiceResult(false, "An internal error occurred while retrieving system settings.");
            }
        }

    }
}
