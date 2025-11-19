using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : BaseController<LogController>
    {
        public readonly LogService _logService;

        public LogController(LogService logService, ILogger<LogController> logger) : base(logService, logger)
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
        public async Task<IActionResult> FetchLogs()
        {
            var logs = await _logService.FetchLogs();
            return ToActionResult(logs);
        }

        [HttpGet("user")]
        public async Task<IActionResult> FetchUserLog([FromQuery] string id)
        {
            var logs = await _logService.GetUserRecentActivityListAsync(id);
            return ToActionResult(logs);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> FetchRecentLogs([FromQuery] int count = 10)
        {
            var logs = await _logService.GetRecentActivityListAsync(count);
            return ToActionResult(logs);
        }

    }
}
