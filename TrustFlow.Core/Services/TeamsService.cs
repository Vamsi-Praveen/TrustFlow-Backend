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
    public class TeamsService:BaseService<TeamsService>
    {
        private readonly IMongoCollection<TeamsConfig> _config;
        private readonly HttpClient _httpClient;

        public TeamsService(ILogger<TeamsService> logger, ApplicationContext context, LogService logService, UserContextService contextService) : base(logService, logger, contextService)
        {
            _config = context.TeamsConfig;
            _httpClient = new HttpClient();
        }


        public async Task<ServiceResult> GetTeamsDetailsAsync()
        {
            try
            {
                var config = await _config.Find(t=>t.IsActive == true).FirstOrDefaultAsync();
                return new ServiceResult(true, "Teams Config retrieved successfully.", config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve teams config.");
                return new ServiceResult(false, "An internal error occurred while retrieving configuration.");
            }
        }


        private async Task<ServiceResult> GetAccessTokenAsync()
        {
            try
            {
                var teamsConfig = await _config.Find(t => t.IsActive == true).FirstOrDefaultAsync();

                if (teamsConfig == null)
                {
                    _logger.LogInformation("Teams Config is unavailable or not found");
                    return new ServiceResult(false, "Teams Config Not Found", null);
                }


                var tokenUrl = teamsConfig.TokenUrl;

                tokenUrl = tokenUrl.Replace("{tenantId}", teamsConfig.TenantId);

                var body = new Dictionary<string, string>
                {
                    { "grant_type", teamsConfig.GrantType },
                    { "client_id", teamsConfig.ClientId },
                    { "client_secret", teamsConfig.ClientSecret },
                    { "scope",teamsConfig.Scope }
                };

                var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(body));
                var json = await response.Content.ReadAsStringAsync();


                var result = JsonDocument.Parse(json);
                var accessToken = result.RootElement.GetProperty("access_token").GetString();

                var tokenResponse = new TeamsTokenResponse
                {
                    AccessToken = accessToken
                };
                return new ServiceResult(true, "Success Generating Token", JsonSerializer.Serialize(tokenResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new ServiceResult(false, "Internal Error occcured", null);
            }
        }

        public async Task<ServiceResult> GetUserIdAsync(string userEmail, string accessToken)
        {
            using var httpClient = new HttpClient();
            var res = await GetAccessTokenAsync();
            var access_token = JsonSerializer.Deserialize<TeamsTokenResponse>(res.Result.ToString()).AccessToken;

            if (string.IsNullOrEmpty(access_token))
            {
                _logger.LogInformation("Access token cannot be found");
                return new ServiceResult(false, "Access token cannot be found", null);
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            var response = await httpClient.GetAsync($"https://graph.microsoft.com/v1.0/users/{userEmail}");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get user ID: {response.StatusCode}\n{json}");
                return new ServiceResult(false, "Failed to get user id");
            }

            var doc = JsonDocument.Parse(json);
            return new ServiceResult(true, "Succesfully Retrived UserId", new { UserId = doc.RootElement.GetProperty("id").GetString() });
        }

        public async Task<ServiceResult> UpdateTeamsConfig(TeamsConfig config)
        {
            try
            {
                var existingTeamsResult = await GetTeamsDetailsAsync();

                if (!existingTeamsResult.Success || existingTeamsResult.Result is not TeamsConfig existingTeams)
                {
                    _logger.LogError("Teams configuration not found in system settings.");
                    return new ServiceResult(false, "Teams configuration not found.");
                }

                var filter = Builders<TeamsConfig>.Filter.Eq(i => i.Id, existingTeams.Id);
                var update = Builders<TeamsConfig>.Update
                    .Set(x => x.TenantId, config.TenantId)
                    .Set(x => x.ClientId, config.ClientId)
                    .Set(x => x.ClientSecret, config.ClientSecret)
                    .Set(x => x.Scope, config.Scope)
                    .Set(x => x.GrantType, config.GrantType)
                    .Set(x => x.TokenUrl, config.TokenUrl)
                    .Set(x => x.IsActive, true)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var result = await _config.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Teams configuration updated successfully.");
                    return new ServiceResult(true, "Teams configuration updated successfully.");
                }

                return new ServiceResult(false, "No changes were made to the Teams configuration.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating Teams configuration in System Settings.");
                return new ServiceResult(false, "An internal error occurred while updating Teams configuration.");
            }
        }


        public async Task<ServiceResult> SendDirectMessageAsync(Notification notification)
        {
            var res = await GetAccessTokenAsync();
            var access_token = JsonSerializer.Deserialize<TeamsTokenResponse>(res.Result.ToString()).AccessToken;

            if (string.IsNullOrEmpty(access_token))
            {
                _logger.LogInformation("Access token cannot be found");
                return new ServiceResult(false, "Access token cannot be found", null);
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            var chatRequest = new CreateChatRequest
            {
                ChatType = "oneOnOne",
                Members = new[]
                 {
                    new ChatMember
                    {
                        ODataType = "#microsoft.graph.aadUserConversationMember",
                        Roles = new[] { "owner" },
                        UserBind = $"https://graph.microsoft.com/v1.0/users('{notification.AssignedUserEmail}')"
                    }
                }
            };

            var chatContent = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json");
            var chatResponse = await _httpClient.PostAsync("https://graph.microsoft.com/v1.0/chats", chatContent);
            var chatJson = await chatResponse.Content.ReadAsStringAsync();

            if (!chatResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create chat {0}", chatJson);
                return new ServiceResult(false, "Failed to create teams chat", null);
            }

            var chatId = JsonDocument.Parse(chatJson).RootElement.GetProperty("id").GetString();

            var messagePayload = new
            {
                body = new
                {
                    contentType = "html",
                    content =
                         "<p>🔔 <b>Bug Assigned!</b> 🐞</p>" +
                         $"<p><b>Project:</b> {notification.ProjectName}<br>" +
                         $"<b>Title:</b> {notification.IssueName}<br>" +
                         $"<b>Description:</b> {notification.IssueDescription}<br>" +
                         $"<b>Assigned To:</b> {notification.AssignedUserName}<br>" +
                         $"<b>Reported By:</b> {notification.ReportedUserName}<br>" +
                         $"<a href='http://localhost/issues'>View in TrustFlow</a></p>"
                }
            };

            var msgContent = new StringContent(JsonSerializer.Serialize(messagePayload), Encoding.UTF8, "application/json");
            var msgResponse = await _httpClient.PostAsync($"https://graph.microsoft.com/v1.0/chats/{chatId}/messages", msgContent);

            if (msgResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message Sent Succesfully to user");
                return new ServiceResult(true, "Teams message sent successfull", null);
            }

            var msgError = await msgResponse.Content.ReadAsStringAsync();
            _logger.LogError($"Failed to send message: {msgError}");
            return new ServiceResult(false, "Failed to send message", null);
        }

    }
}
