using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Models;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityController : BaseController
    {
        private readonly ActivityService _activityService;
        private readonly ILogger<ActivityController> _logger;

        public ActivityController(ActivityService activityService, ILogger<ActivityController> logger)
        {
            _activityService = activityService;
            _logger = logger;
        }

        private IActionResult ToActionResult(ServiceResult result)
        {
            if (result.Success)
            {
                return Ok(new APIResponse(true, result.Message, result.Result));
            }

            if (result.Message.Contains("An internal error"))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new APIResponse(false, result.Message));
            }
            return BadRequest(new APIResponse(false, result.Message));
        }

        [HttpPut("send-activity")]
        public async Task<IActionResult> SendActivity([FromBody] ActivityLog activityLog)
        {
            var result = await _activityService.SendActivity(activityLog);
            return ToActionResult(result);
        }

        [HttpPost("recieve-activity")]
        public async Task<IActionResult> RecieveActivity()
        {
            var result = await _activityService.RecieveActivity();
            return ToActionResult(result);
        }
    }
}
