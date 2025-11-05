using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Health()
        {
            return Ok(new APIResponse(true, "Service is running", null));
        }
    }
}
