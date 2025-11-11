using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrustFlow.Core.Communication;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
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

        [HttpPatch("profile/{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileDTO updatedUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid user data provided.", ModelState));
            }

            var result = await _userService.UpdateProfileAsync(id, updatedUser);
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

        [AllowAnonymous]
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

            if (!result.Success)
                return Unauthorized(new APIResponse(false, result.Message));

            var user = (User)result.Result;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(8)
                });

            return Ok(new APIResponse(true, "Login successful.", new
            {
                user.Username,
                user.Email,
                user.Role
            }));
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new APIResponse(true, "You have been logged out."));
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new APIResponse(false, "Not authenticated."));

            var result = await _userService.GetCompleteUserById(userId);
            if (result == null)
                return Unauthorized(new APIResponse(false, "User not found."));

            return Ok(new APIResponse(true, "User retrieved successfully.", result));
        }


        [HttpPost("changepassword")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword changePasswordRequest)
        {
            var userId = Id;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new APIResponse(false, "Not authenticated."));

            changePasswordRequest.UserId = userId;

            var result = await _userService.ChangePasswordAsync(changePasswordRequest.UserId, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword);

            return ToActionResult(result);

        }


        [HttpPut("notificationsettings")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] UserNotification notificationSettings)
        {
            var userId = Id;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new APIResponse(false, "User Not authenticated."));
            var result = await _userService.UpdateUserNotificationConfig(userId,notificationSettings);
            return ToActionResult(result);
        }

        [HttpGet("notificationsettings")]
        [ProducesResponseType(typeof(APIResponse),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse),StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> FetchUserNotificationSettings()
        {
            var userId = Id;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new APIResponse(false, "User Not authenticated."));
            }
            var result = await _userService.GetUserNotificationConfig(userId);
            return ToActionResult(result);
        }
    }
}
