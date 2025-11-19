using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectDetailsController : BaseController<ProjectDetailsController>
    {
        public readonly ProjectDetailsService _projectDetailsService;

        public ProjectDetailsController(ProjectDetailsService projectDetailsService, LogService log, ILogger<ProjectDetailsController> logger) : base(log, logger)
        {
            _projectDetailsService = projectDetailsService;
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


        [HttpGet("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectOverview(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new APIResponse(false, "Id Required"));
            }
            var result = await _projectDetailsService.GetProjectOverview(id);
            return ToActionResult(result);
        }
    }
}
