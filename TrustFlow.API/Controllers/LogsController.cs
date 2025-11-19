using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : BaseController
    {
        public readonly LogService _logService;

        public LogsController(LogService logService)
        {
            _logService = logService;
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

        [HttpGet]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FetchLogs()
        {
            var logs = await _logService.FetchLogs();
            return ToActionResult(logs);
        }

        [HttpGet("user")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FetchUserLog([FromQuery] string id)
        {
            var logs = await _logService.GetUserRecentActivityListAsync(id);
            return ToActionResult(logs);
        }

        [HttpGet("recent")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FetchRecentLogs([FromQuery] int count = 10)
        {
            var logs = await _logService.GetRecentActivityListAsync(count);
            return ToActionResult(logs);
        }

        [HttpGet("projectactivity")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FetchProjectRelatedActivity([FromQuery] string projectId, int count = 10)
        {
            var logs = await _logService.GetProjectRecentActivityListAsync(projectId, count);
            return ToActionResult(logs);
        }

    }
}
