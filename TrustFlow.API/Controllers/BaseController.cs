using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrustFlow.Core.Models;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    public abstract class BaseController<T> : ControllerBase
    {
        public readonly LogService _logService;
        private readonly ILogger<T> _logger;
        public BaseController(LogService logService, ILogger<T> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        protected string Id =>
            HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected string Email =>
            HttpContext.User.FindFirstValue(ClaimTypes.Email);

        protected string AccessToken =>
           HttpContext.User.Claims.FirstOrDefault(c => c.Type == "Access_Token").Value;

        protected string? IpAddress =>
            HttpContext.Connection.RemoteIpAddress?.ToString();

        protected string? UserAgent =>
            Request.Headers["User-Agent"].ToString();

        [NonAction]
        protected async Task LogAsync(ActivityLog log)
        {
            try
            {
                log.UserId = log.UserId ?? Id;
                log.IpAddress = IpAddress;
                log.UserAgent = UserAgent;

                await _logService.Pushlog(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write activity log for user {UserId}", log.UserId);
            }
        }
    }
}
