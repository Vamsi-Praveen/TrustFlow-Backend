using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly SlackService _slackService;
        private readonly TeamsService _teamsService;

        public IntegrationController(SlackService slackService, TeamsService teamsService)
        {
            _slackService = slackService;
            _teamsService = teamsService;
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


        [HttpPost("slack")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendSlackNotification(Notification notification)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid data provided.", ModelState));
            }

            var result = await _slackService.SendSlackNotification(notification);

            return ToActionResult(result);
        }

        [HttpPost("slack/dm")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendSlackDMNotification(Notification notification)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid data provided.", ModelState));
            }


            if (string.IsNullOrEmpty(notification.AssignedUserEmail))
            {
                return BadRequest(new APIResponse(false, "AssignedUserEmail is not provided", null));
            }

            var result = await _slackService.SendSlackDMNotification(notification);

            return ToActionResult(result);
        }

        [HttpPost("teams/dm")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendTeamsDMNotification(Notification notification)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid data provided.", ModelState));
            }


            if (string.IsNullOrEmpty(notification.AssignedUserEmail))
            {
                return BadRequest(new APIResponse(false, "AssignedUserEmail is not provided", null));
            }

            var result = await _teamsService.SendDirectMessageAsync(notification);

            return ToActionResult(result);
        }

    }
}
