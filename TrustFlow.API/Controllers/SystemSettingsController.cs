using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Models;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingsController : BaseController
    {
        private readonly SystemSettingService _systemSettingService;
        private readonly SlackService _slackService;
        private readonly TeamsService _teamsService;
        private readonly EmailService _emailService;

        public SystemSettingsController(SystemSettingService systemSettingService,SlackService slackService,TeamsService teamsService,EmailService emailService)
        {
            _systemSettingService = systemSettingService;
            _slackService = slackService;
            _teamsService = teamsService;
            _emailService = emailService;

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

        [HttpPut("update-slack")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateSlackConfig(SlackConfig config)
        {
            var result = await _slackService.UpdateSlackConfig(config);
            return ToActionResult(result);
        }

        [HttpPut("update-smtp")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateSmtpConfig(SMTPConfig config)
        {
            var result = await _emailService.UpdateSmtpConfig(config);
            return ToActionResult(result);
        }

        [HttpPut("update-teams")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateTeamsConfig(TeamsConfig config)
        {
            var result = await _teamsService.UpdateTeamsConfig(config);
            return ToActionResult(result);
        }

        [HttpPut("update-config")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdatePortalConfig(PortalConfig config)
        {
            var result = await _systemSettingService.UpdatePortalConfig(config);
            return ToActionResult(result);
        }

    }
}
