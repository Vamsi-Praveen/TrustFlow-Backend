using Microsoft.Extensions.Logging;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public abstract class BaseService<T>
    {
        public readonly LogService _logService;
        public readonly ILogger<T> _logger;
        public readonly UserContextService _userContextService;

        public BaseService(LogService logService, ILogger<T> logger, UserContextService userContextService)
        {
            _logService = logService;
            _logger = logger;
            _userContextService = userContextService;
        }

        protected async Task SendLogAsync(ActivityLog log)
        {
            try
            {
                if (log == null)
                {
                    _logger.LogWarning("Attempted to write a null activity log.");
                    return;
                }

                await _logService.Pushlog(log);

                _logger.LogInformation(
                    "Activity log written"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write activity log for user {UserId}", log.UserId);
            }
        }
    }
}
