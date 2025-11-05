using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Models;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(ProjectService projectService, ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _logger = logger;
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
            if (result.Message.Contains("already exists"))
            {
                return Conflict(new APIResponse(false, result.Message));
            }
            if (result.Message.Contains("already a member"))
            {
                return Conflict(new APIResponse(false, result.Message));
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
            var result = await _projectService.GetProjectsAsync();
            return ToActionResult(result);
        }


        [HttpGet("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _projectService.GetProjectByIdAsync(id);
            return ToActionResult(result);
        }


        [HttpPost]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] Project newProject)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid project data provided.", ModelState));
            }

            var result = await _projectService.CreateAsync(newProject);

            if (result.Success)
            {
                var createdProject = result.Result as Project;
                return CreatedAtAction(nameof(Get), new { id = createdProject?.Id }, new APIResponse(true, result.Message, createdProject));
            }

            return ToActionResult(result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(string id, [FromBody] Project updatedProject)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid project data provided.", ModelState));
            }

            var result = await _projectService.UpdateAsync(id, updatedProject);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _projectService.DeleteAsync(id);
            return ToActionResult(result);
        }

        [HttpPost("{projectId}/members")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddMember(string projectId, [FromBody] ProjectMember newMember)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid member data provided.", ModelState));
            }

            var result = await _projectService.AddMemberToProjectAsync(projectId, newMember);
            return ToActionResult(result);
        }

        [HttpDelete("{projectId}/members/{userId}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveMember(string projectId, string userId)
        {
            var result = await _projectService.RemoveMemberFromProjectAsync(projectId, userId);
            return ToActionResult(result);
        }
    }
}
