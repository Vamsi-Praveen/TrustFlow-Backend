using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TrustFlow.API.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        public string Id
        {
            get { return HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value; }
        }

        public string Email
        {
            get { return HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value; }
        }

        public string AccessToken
        {
            get { return HttpContext.User.Claims.FirstOrDefault(c => c.Type == "Access_Token").Value; }
        }

        protected string? IpAddress =>
            HttpContext.Connection.RemoteIpAddress?.ToString();

        protected string? UserAgent =>
            Request.Headers["User-Agent"].ToString();
    }
}
