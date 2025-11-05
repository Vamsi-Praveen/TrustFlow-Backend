using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private IActionResult ToActionResult(ServiceResult result)
        {
            if (result.Result != null)
            {
                if (result.Result is User user)
                {
                    user.PasswordHash = null;
                }
                else if (result.Result is List<User> users)
                {
                    users.ForEach(u => u.PasswordHash = null);
                }
            }

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
            if (result.Message.Contains("already exists"))
            {
                return Conflict(new APIResponse(false, result.Message));
            }
            if (result.Message.Contains("Invalid") || result.Message.Contains("inactive"))
            {
                return Unauthorized(new APIResponse(false, result.Message));
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
        public async Task<IActionResult> Get()
        {
            var result = await _userService.GetUsersAsync();
            return ToActionResult(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            return ToActionResult(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] User newUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid user data provided.", ModelState));
            }

            var result = await _userService.CreateAsync(newUser);

            if (result.Success)
            {
                var createdUser = result.Result as User;
                if (createdUser != null) createdUser.PasswordHash = null;
                return CreatedAtAction(nameof(Get), new { id = createdUser?.Id }, new APIResponse(true, result.Message, createdUser));
            }

            return ToActionResult(result);
        }


        [HttpPut("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(string id, [FromBody] User updatedUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid user data provided.", ModelState));
            }

            var result = await _userService.UpdateAsync(id, updatedUser);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _userService.DeleteAsync(id);
            return ToActionResult(result);
        }


        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Authenticate([FromBody] LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Username and Password are required.", ModelState));
            }

            var result = await _userService.AuthenticateAsync(loginRequest.Username, loginRequest.Password);

            return ToActionResult(result);
        }
    }
}