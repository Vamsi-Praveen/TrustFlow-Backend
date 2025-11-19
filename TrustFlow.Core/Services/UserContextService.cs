using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace TrustFlow.Core.Services
{
    public class UserContextService
    {
        private readonly IHttpContextAccessor _context;

        public UserContextService(IHttpContextAccessor context)
        {
            _context = context;
        }

        public string? UserId =>
             _context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;

        public string? Email =>
            _context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;

        public string? IpAddress =>
            _context.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        public string? UserAgent =>
            _context.HttpContext?.Request?.Headers["User-Agent"];
    }
}
