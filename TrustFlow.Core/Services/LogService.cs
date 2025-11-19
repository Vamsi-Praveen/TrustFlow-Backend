using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class LogService
    {
        private readonly IMongoCollection<ActivityLog> _logCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly ILogger<LogService> _logger;
        public LogService(ApplicationContext context,
            ILogger<LogService> logger)
        {
            _logCollection = context.ActivityLog;
            _logger = logger;
            _userCollection = context.Users;
        }

        public string GetTimeDifference(DateTime inputDate)
        {
            var now = DateTime.UtcNow;
            var diff = now - inputDate.ToUniversalTime();

            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} minute{(diff.TotalMinutes >= 2 ? "s" : "")} ago";

            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hour{(diff.TotalHours >= 2 ? "s" : "")} ago";

            if (diff.TotalDays < 30)
                return $"{(int)diff.TotalDays} day{(diff.TotalDays >= 2 ? "s" : "")} ago";

            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)} month{(diff.TotalDays / 30 >= 2 ? "s" : "")} ago";

            return $"{(int)(diff.TotalDays / 365)} year{(diff.TotalDays / 365 >= 2 ? "s" : "")} ago";
        }

        public async Task<ServiceResult> Pushlog(ActivityLog log)
        {
            try
            {
                await _logCollection.InsertOneAsync(log);
                return new ServiceResult(true, "Log pushed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing log");
                return new ServiceResult(false, "Error pushing log: " + ex.Message);
            }
        }


        public async Task<ServiceResult> FetchLogs(int limit = 100)
        {
            try
            {
                var logs = await _logCollection.Find(_ => true)
                                               .SortByDescending(log => log.Timestamp)
                                               .Limit(limit)
                                               .ToListAsync();
                return new ServiceResult(true, "Logs fetched successfully", logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs");
                return new ServiceResult(false, "Error fetching logs: " + ex.Message);
            }
        }

        public async Task<ServiceResult> GetRecentActivityListAsync(int count)
        {
            try
            {
                var logs = await _logCollection.Find(_ => true)
                                               .SortByDescending(log => log.Timestamp)
                                               .Limit(count)
                                               .ToListAsync();

                var recentLogs = new List<RecentActivityDTO>();

                var usersList = await _userCollection.Find(_ => true).ToListAsync();

                var usersDictionary = usersList.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

                foreach (var log in logs)
                {
                    string userName;

                    if (!usersDictionary.TryGetValue(log.UserId, out userName))
                    {
                        userName = "System";
                    }

                    recentLogs.Add(new RecentActivityDTO
                    {
                        Id= log.Id,
                        User = userName,
                        Action = log.Description,
                        Date = log.Timestamp.ToString("MMM d, yyyy")
                    });
                }
                
                return new ServiceResult(true, "Recent activity fetched successfully", recentLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activity");
                return new ServiceResult(false, "Error fetching recent activity: " + ex.Message);
            }
        }

        public async Task<ServiceResult> GetUserRecentActivityListAsync(string userId, int count=5)
        {
            try
            {
                var logs = await _logCollection.Find(log => log.UserId == userId)
                                               .SortByDescending(log => log.Timestamp)
                                               .Limit(count)
                                               .ToListAsync();

                List<object> recentLogs = new List<object>();

                foreach (var log in logs)
                {
                    recentLogs.Add(new
                    {
                        log.Description,
                        Date = GetTimeDifference(log.Timestamp)
                    });
                }

                return new ServiceResult(true, "User recent activity fetched successfully", recentLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user recent activity");
                return new ServiceResult(false, "Error fetching user recent activity: " + ex.Message);
            }
        }
    }
}
