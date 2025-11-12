using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class SlackService
    {
        private readonly ILogger<SlackService> _logger;
        private readonly IMongoCollection<SlackConfig> _config;
        private readonly HttpClient _httpClient;

        public SlackService(ILogger<SlackService> logger, ApplicationContext context)
        {
            _logger = logger;
            _config = context.SlackConfig;
            _httpClient = new HttpClient();
        }


        public async Task<ServiceResult> GetSlackDetailsAsync()
        {
            try
            {
                var config = await _config.Find(c=>c.IsActive == true).FirstOrDefaultAsync();
                return new ServiceResult(true, "Slack Config retrieved successfully.", config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve slack config.");
                return new ServiceResult(false, "An internal error occurred while retrieving configuration.");
            }
        }

        public async Task<ServiceResult> CreateSlackConfigAsync(SlackConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.SlackChannelName))
                {
                    _logger.LogWarning("Attempted to create config with missing name.");
                    return new ServiceResult(false, "SlackChannel name is required.");
                }

                if (string.IsNullOrWhiteSpace(config.SlackAppName))
                {
                    _logger.LogWarning("Attempted to create config with missing name.");
                    return new ServiceResult(false, "SlackApplication name is required.");
                }

                if (string.IsNullOrWhiteSpace(config.SlackWebhookURL))
                {
                    _logger.LogWarning("Attempted to create config with missing webhook url.");
                    return new ServiceResult(false, "Slackwebhook url is required.");
                }
                await _config.InsertOneAsync(config);
                return new ServiceResult(true, "Slack Config created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create");
                return new ServiceResult(false, "An internal error occurred while creating the config.");
            }
        }

        public async Task<ServiceResult> UpdateSlackConfig(SlackConfig config)
        {
            try
            {
                var existingSlackResult = await GetSlackDetailsAsync();

                if (!existingSlackResult.Success || existingSlackResult.Result is not SlackConfig existingSlack)
                {
                    _logger.LogError("Slack configuration not found in system settings.");
                    return new ServiceResult(false, "Slack configuration not found.");
                }

                var filter = Builders<SlackConfig>.Filter.Eq(i => i.Id, existingSlack.Id);
                var update = Builders<SlackConfig>.Update
                    .Set(x => x.SlackChannelName, config.SlackChannelName)
                    .Set(x => x.SlackBotToken, config.SlackBotToken)
                    .Set(x => x.SlackBotName, config.SlackBotName)
                    .Set(x => x.SlackAppName, config.SlackAppName)
                    .Set(x => x.SlackWebhookURL, config.SlackWebhookURL)
                    .Set(x => x.SlackBaseAddress, config.SlackBaseAddress)
                    .Set(x => x.IsActive, true)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var result = await _config.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Slack configuration updated successfully.");
                    return new ServiceResult(true, "Slack configuration updated successfully.");
                }

                return new ServiceResult(false, "No changes were made to the Slack configuration.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating Slack configuration in System Settings.");
                return new ServiceResult(false, "An internal error occurred while updating Slack configuration.");
            }
        }


        public async Task<ServiceResult> SendSlackNotification(Notification notification)
        {
            try
            {
                var slack = await _config.Find(c => c.IsActive == true).FirstOrDefaultAsync();

                if (slack == null)
                {
                    _logger.LogInformation("Slack Config is unavailable or not found");
                    return new ServiceResult(false, "Slack Config Not Found", null);
                }

                var slackWebhookURL = slack.SlackWebhookURL;

                if (string.IsNullOrEmpty(slackWebhookURL))
                {
                    _logger.LogInformation("Slack Webhook url is not found");
                    return new ServiceResult(false, "Slack webhook url not found");
                }

                var message = new
                {
                    text = $":bell: *Bug Assigned!* :beetle:\n" +
                           $"*Project:* {notification.ProjectName}\n" +
                           $"*Title:* {notification.IssueName}\n" +
                           $"*Description:* {notification.IssueDescription}\n" +
                           $"*Assigned To:* {notification.AssignedUserName}\n" +
                           $"*Reported By:* {notification.ReportedUserName}\n" +
                           $"<http://localhost/issues|View in TrustFlow>"
                };

                var payload = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(slackWebhookURL, payload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Slack notification failed. Status: {response.StatusCode}");
                    return new ServiceResult(false, $"Failed to send notification: {response.StatusCode}");
                }

                _logger.LogInformation("Slack notification sent successfully.");
                return new ServiceResult(true, "Slack notification sent successfully", null);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending Slack notification");
                return new ServiceResult(false, "An internal error occurred while sending the Slack notification");
            }
        }


        public async Task<ServiceResult> SendSlackDMNotification(Notification notification)
        {
            try
            {
                var slackDmConfig = await _config.Find(c => c.IsActive == true).FirstOrDefaultAsync();

                if (slackDmConfig == null)
                {
                    _logger.LogInformation("Slack Config is unavailable or not found");
                    return new ServiceResult(false, "Slack Config Not Found", null);
                }

                var botToken = slackDmConfig.SlackBotToken;

                if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(slackDmConfig.SlackBaseAddress))
                {
                    _logger.LogInformation("Slack Bot token or slackBase address is unavailable or not found");
                    return new ServiceResult(false, "Slack Config Not Found", null);
                }

                HttpClient _slackClient = new HttpClient()
                {
                    BaseAddress = new Uri(slackDmConfig.SlackBaseAddress)
                };

                _slackClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

                var userResponse = await _slackClient.GetAsync($"users.lookupByEmail?email={notification.AssignedUserEmail}");
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userObj = JsonDocument.Parse(userJson);
                var userId = userObj.RootElement.GetProperty("user").GetProperty("id").GetString();


                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning($"Slack user not found for {notification.AssignedUserEmail}");
                    return new ServiceResult(false,"Slack User Not Found",null);
                }

                var dmPayload = new StringContent(
                    JsonSerializer.Serialize(new { users = userId }),
                    Encoding.UTF8,
                    "application/json"
                );

                var dmResponse = await _slackClient.PostAsync("conversations.open", dmPayload);
                var dmJson = await dmResponse.Content.ReadAsStringAsync();
                var dmObj = JsonDocument.Parse(dmJson);
                var channelId = dmObj.RootElement.GetProperty("channel").GetProperty("id").GetString();

                var messagePayload = new StringContent(
                   JsonSerializer.Serialize(new
                   {
                       channel = channelId,
                       text = $":bell: *Bug Assigned!* :beetle:\n" +
                           $"*Project:* {notification.ProjectName}\n" +
                           $"*Title:* {notification.IssueName}\n" +
                           $"*Description:* {notification.IssueDescription}\n" +
                           $"*Assigned To:* {notification.AssignedUserName}\n" +
                           $"*Reported By:* {notification.ReportedUserName}\n" +
                           $"<http://localhost/issues|View in TrustFlow>"
                   }),
                   Encoding.UTF8,
                   "application/json"
               );

                var messageResponse = await _slackClient.PostAsync("chat.postMessage", messagePayload);
                var success = messageResponse.IsSuccessStatusCode;


                if (!success)
                {
                    var errorContent = await messageResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send Slack DM: {errorContent}");
                    return new ServiceResult(false, "Failed to send Slack DM");
                }

                return new ServiceResult(true, "Slack DM sent successful", null);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending Slack notification");
                return new ServiceResult(false, "An internal error occurred while sending the Slack notification");
            }
        }


    }
}
