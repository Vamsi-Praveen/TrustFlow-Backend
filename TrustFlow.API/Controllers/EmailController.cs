using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : BaseController<EmailController>
    {
        private readonly EmailService _emailService;

        public EmailController(EmailService emailService, LogService logService, ILogger<EmailController> logger) : base(logService, logger)
        {
            _emailService = emailService;
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


        [HttpGet("test-smtp")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TestSmtp()
        {
            var result = await _emailService.TestSMTP();
            return ToActionResult(result);
        }

        [HttpPost("send")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid request data provided.", ModelState));
            }

            var result = await _emailService.SendEmailAsync(request);
            return ToActionResult(result);
        }


        [HttpPost("sendRegistrationMail")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendRegistrationMail([FromBody] EmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid request data provided.", ModelState));
            }

            var result = await _emailService.SendRegistrationMailAsync(request);
            return ToActionResult(result);
        }

        [AllowAnonymous]
        [HttpPost("SendPasswordResetMail")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendPasswordReset([FromBody] EmailRequest passwordReset)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid data provided.", ModelState));
            }
            var result = await _emailService.SendPasswordResetMailAsync(passwordReset);
            return ToActionResult(result);
        }
    }
}