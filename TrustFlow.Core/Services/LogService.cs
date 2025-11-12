using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class LogService
    {
        private readonly ApplicationContext _context;
        private readonly IMongoCollection<ActivityLog> _logCollection;
        private readonly ILogger<LogService> _logger;
        public LogService(ApplicationContext context, ILogger<LogService> logger)
        {
            _logCollection = _context.ActivityLog;
            _context = context;
            _logger = logger;
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
    }
}
