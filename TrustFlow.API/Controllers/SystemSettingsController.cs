using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingsController : BaseController
    {
        private readonly SystemSettingService _systemSettingService;

        public SystemSettingsController(SystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        private IActionResult ToActionResult(ServiceResult result)
        {
            if (result.Success)
            {
                if (result.Result != null)
                {
                    return Ok(new APIResponse(true, result.Message, result.Result));
                }
                return Ok(new APIResponse(true, result.Message));
            }

            if (result.Message.Contains("not found"))
            {
                return NotFound(new APIResponse(false, result.Message));
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
        public async Task<IActionResult> GetSystemSettings()
        {
            var result = await _systemSettingService.GetSystemSettings();
            return ToActionResult(result);
        }

    }
}
