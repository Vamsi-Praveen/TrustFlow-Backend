using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class SystemSettingService:BaseService<SystemSettingService>
    {
        private readonly TeamsService _teamsService;
        private readonly SlackService _slackService;
        private readonly EmailService _emailService;
        private readonly IMongoCollection<PortalConfig> _portalConfig;
        private readonly IMongoCollection<IssueStatus> _issueStatuses;
        private readonly IMongoCollection<IssuePriority> _issuePriority;
        private readonly IMongoCollection<IssueSeverity> _issueSeverity;
        private readonly IMongoCollection<IssueType> _issueType;

        public SystemSettingService(TeamsService teamsService, SlackService slackService, EmailService emailService, ILogger<SystemSettingService> logger, ApplicationContext context,LogService logService, UserContextService contextService) :base(logService,logger, contextService)
        {
            _teamsService = teamsService;
            _slackService = slackService;
            _emailService = emailService;
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
                    .Project(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync();

                var issuePriority = await _issuePriority.Find(_ => true)
                    .Project(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync();

                var issueTypes = await _issueType.Find(_ => true)
                    .Project(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync();

                var issueSeverity = await _issueSeverity.Find(_ => true)
                    .Project(x => new { x.Id, x.Name, x.Description })
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

        public async Task<ServiceResult> CreateIssueConfigurations(IssueConfigurations config)
        {
            try
            {
                if (config == null)
                    return new ServiceResult(false, "Invalid request body.");

                if (string.IsNullOrWhiteSpace(config.ItemType))
                    return new ServiceResult(false, "ItemType is required.");

                if (string.IsNullOrWhiteSpace(config.Name))
                    return new ServiceResult(false, "Name is required.");

                string configType = config.ItemType.ToLower().Trim();
                string name = config.Name.Trim();

                if (configType == "statuses")
                {
                    var exists = await _issueStatuses.Find(x => x.Name.ToLower() == name.ToLower()).FirstOrDefaultAsync();
                    if (exists != null)
                        return new ServiceResult(false, "Status name already exists.");

                    var newItem = new IssueStatus
                    {
                        Name = name,
                        Description = config.Description,
                    };

                    await _issueStatuses.InsertOneAsync(newItem);

                    return new ServiceResult(true, "Issue status created successfully.", newItem);
                }

                if (configType == "priorities")
                {
                    var exists = await _issuePriority.Find(x => x.Name.ToLower() == name.ToLower()).FirstOrDefaultAsync();
                    if (exists != null)
                        return new ServiceResult(false, "Priority name already exists.");

                    var newItem = new IssuePriority
                    {
                        Name = name,
                        Description = config.Description,
                    };

                    await _issuePriority.InsertOneAsync(newItem);

                    return new ServiceResult(true, "Issue priority created successfully.", newItem);
                }

                if (configType == "severities")
                {
                    var exists = await _issueSeverity.Find(x => x.Name.ToLower() == name.ToLower()).FirstOrDefaultAsync();
                    if (exists != null)
                        return new ServiceResult(false, "Severity name already exists.");

                    var newItem = new IssueSeverity
                    {
                        Name = name,
                        Description = config.Description,
                    };

                    await _issueSeverity.InsertOneAsync(newItem);

                    return new ServiceResult(true, "Issue severity created successfully.", newItem);
                }


                if (configType == "types")
                {
                    var exists = await _issueType.Find(x => x.Name.ToLower() == name.ToLower()).FirstOrDefaultAsync();
                    if (exists != null)
                        return new ServiceResult(false, "Issue type already exists.");

                    var newItem = new IssueType
                    {
                        Name = name,
                        Description = config.Description,
                    };

                    await _issueType.InsertOneAsync(newItem);

                    return new ServiceResult(true, "Issue type created successfully.", newItem);
                }

                return new ServiceResult(false, $"Unknown Issue Configuration type '{config.ItemType}'. Allowed: statuses, priorities, severities, types.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating Issue Configuration for {config.ItemType}: {ex}");
                return new ServiceResult(false, "An internal error occurred while creating issue configuration.");
            }
        }

        public async Task<ServiceResult> UpdateIssueConfigurations(IssueConfigurations config)
        {
            try
            {
                if (config == null || string.IsNullOrWhiteSpace(config.ItemType) ||
                    string.IsNullOrWhiteSpace(config.Name) || string.IsNullOrWhiteSpace(config.Id))
                {
                    return new ServiceResult(false, "Invalid request. Id, Name, and ItemType are required.");
                }

                string configType = config.ItemType.ToLower().Trim();
                string id = config.Id.Trim();
                string name = config.Name.Trim();

                switch (configType)
                {
                    case "statuses":
                        var existingStatus = await _issueStatuses.Find(x => x.Id == id).FirstOrDefaultAsync();
                        if (existingStatus == null)
                            return new ServiceResult(false, "Status not found.");

                        var duplicateStatus = await _issueStatuses.Find(x => x.Name.ToLower() == name.ToLower() && x.Id != id).FirstOrDefaultAsync();
                        if (duplicateStatus != null)
                            return new ServiceResult(false, $"Status '{name}' already exists.");

                        var updateStatus = Builders<IssueStatus>.Update
                            .Set(x => x.Name, name)
                            .Set(x => x.Description, config.Description)
                            .Set(x => x.UpdatedAt, DateTime.UtcNow);

                        await _issueStatuses.UpdateOneAsync(x => x.Id == id, updateStatus);
                        break;

                    case "priorities":
                        var existingPriority = await _issuePriority.Find(x => x.Id == id).FirstOrDefaultAsync();
                        if (existingPriority == null)
                            return new ServiceResult(false, "Priority not found.");

                        var duplicatePriority = await _issuePriority.Find(x => x.Name.ToLower() == name.ToLower() && x.Id != id).FirstOrDefaultAsync();
                        if (duplicatePriority != null)
                            return new ServiceResult(false, $"Priority '{name}' already exists.");

                        var updatePriority = Builders<IssuePriority>.Update
                            .Set(x => x.Name, name)
                            .Set(x => x.Description, config.Description)
                            .Set(x => x.UpdatedAt, DateTime.UtcNow);

                        await _issuePriority.UpdateOneAsync(x => x.Id == id, updatePriority);
                        break;

                    case "severities":
                        var existingSeverity = await _issueSeverity.Find(x => x.Id == id).FirstOrDefaultAsync();
                        if (existingSeverity == null)
                            return new ServiceResult(false, "Severity not found.");

                        var duplicateSeverity = await _issueSeverity.Find(x => x.Name.ToLower() == name.ToLower() && x.Id != id).FirstOrDefaultAsync();
                        if (duplicateSeverity != null)
                            return new ServiceResult(false, $"Severity '{name}' already exists.");

                        var updateSeverity = Builders<IssueSeverity>.Update
                            .Set(x => x.Name, name)
                            .Set(x => x.Description, config.Description)
                            .Set(x => x.UpdatedAt, DateTime.UtcNow);

                        await _issueSeverity.UpdateOneAsync(x => x.Id == id, updateSeverity);
                        break;

                    case "types":
                        var existingType = await _issueType.Find(x => x.Id == id).FirstOrDefaultAsync();
                        if (existingType == null)
                            return new ServiceResult(false, "Issue Type not found.");

                        var duplicateType = await _issueType.Find(x => x.Name.ToLower() == name.ToLower() && x.Id != id).FirstOrDefaultAsync();
                        if (duplicateType != null)
                            return new ServiceResult(false, $"Issue Type '{name}' already exists.");

                        var updateType = Builders<IssueType>.Update
                            .Set(x => x.Name, name)
                            .Set(x => x.Description, config.Description)
                            .Set(x => x.UpdatedAt, DateTime.UtcNow);

                        await _issueType.UpdateOneAsync(x => x.Id == id, updateType);
                        break;

                    default:
                        return new ServiceResult(false, $"Invalid configuration type '{config.ItemType}'.");
                }

                return new ServiceResult(true, $"Successfully updated {config.ItemType} '{config.Name}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating Issue Configuration for {config?.ItemType}: {ex}");
                return new ServiceResult(false, "An internal error occurred while updating issue configuration.");
            }
        }

        public async Task<ServiceResult> DeleteIssueConfiguration(string id, string itemType)
        {
            try
            {
                string configType = itemType?.ToLower().Trim();
                DeleteResult result = null;

                switch (configType)
                {
                    case "statuses":
                        result = await _issueStatuses.DeleteOneAsync(x => x.Id == id);
                        break;

                    case "priorities":
                        result = await _issuePriority.DeleteOneAsync(x => x.Id == id);
                        break;

                    case "severities":
                        result = await _issueSeverity.DeleteOneAsync(x => x.Id == id);
                        break;

                    case "types":
                        result = await _issueType.DeleteOneAsync(x => x.Id == id);
                        break;

                    default:
                        return new ServiceResult(false, "Unknown configuration type.");
                }

                if (result.DeletedCount == 0)
                    return new ServiceResult(false, "No record found to delete.");

                return new ServiceResult(true, "Configuration deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting Issue Configuration: {ex}");
                return new ServiceResult(false, "An internal error occurred.");
            }
        }

    }
}
