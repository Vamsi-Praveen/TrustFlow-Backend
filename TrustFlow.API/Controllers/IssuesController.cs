using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Models;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : BaseController<IssuesController>
    {
        private readonly IssueService _issueService;

        public IssuesController(IssueService issueService, ILogger<IssuesController> logger, LogService log) : base(log, logger)
        {
            _issueService = issueService;
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
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetIssue(string id)
        {
            var result = await _issueService.GetIssueDetailsAsync(id);
            return ToActionResult(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllIssues()
        {
            var result = await _issueService.GetAllIssuesAsync();
            return ToActionResult(result);
        }

        [HttpGet("filters")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetIssueFilters()
        {
            var result = await _issueService.GetIssuesRelatedFilters();
            return ToActionResult(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateIssue([FromBody] Issue newIssue)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new APIResponse(false, "Invalid issue data provided.", ModelState));
            }

            var result = await _issueService.RaiseIssue(newIssue);

            if (result.Success)
            {
                var createdIssue = result.Result as Issue;
                return CreatedAtAction(nameof(GetAllIssues), new { id = createdIssue?.Id }, new APIResponse(true, result.Message, createdIssue));
            }

            return ToActionResult(result);
        }

        //[HttpPut]
        //[ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> UpdateIssue([FromBody] Issue updatedIssue)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new APIResponse(false, "Invalid issue data provided.", ModelState));
        //    }
        //    var result = await _issueService.EditIssue(updatedIssue);
        //    return ToActionResult(result);
        //}

        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                return BadRequest(new APIResponse(false, "New status cannot be empty."));
            }
            var result = await _issueService.UpdateIssueStatus(id, newStatus);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteIssue(string id)
        {
            var result = await _issueService.DeleteIssue(id);
            return ToActionResult(result);
        }


        //[HttpGet("analytics")]
        //public async Task<IActionResult> GetGlobalAnalytics()
        //{
        //    var result = await _issueService.IssueAnalytics();
        //    return ToActionResult(result);
        //}

        [HttpGet("analytics/project-wise")]
        public async Task<IActionResult> GetProjectWiseAnalytics()
        {
            var result = await _issueService.ProjectWiseIssueAnalytics();
            return ToActionResult(result);
        }

        //[HttpGet("project/{projectId}/analytics")]
        //public async Task<IActionResult> GetProjectAnalytics(string projectId)
        //{
        //    var result = await _issueService.ProjectWiseIssueAnalytics(projectId);
        //    return ToActionResult(result);
        //}


        //[HttpGet("project/{projectId}")]
        //public async Task<IActionResult> GetIssuesByProject(string projectId)
        //{
        //    var result = await _issueService.oss(projectId);
        //    return ToActionResult(result);
        //}

        //[HttpGet("project/{projectId}/status/{status}")]
        //public async Task<IActionResult> GetProjectIssuesByStatus(string projectId, string status)
        //{
        //    var result = await _issueService.GetProjectIssuesByStatus(projectId, status);
        //    return ToActionResult(result);
        //}

        [HttpGet("user/{userId}/assigned")]
        public async Task<IActionResult> GetIssuesAssignedToUser(string userId)
        {
            var result = await _issueService.GetIssuesAssignedToUserAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet("user/{userId}/reported")]
        public async Task<IActionResult> GetIssuesReportedByUser(string userId)
        {
            var result = await _issueService.GetIssuesReportedByUserAsync(userId);
            return ToActionResult(result);
        }

        //[HttpGet("user/{userId}/open-count")]
        //public async Task<IActionResult> GetOpenIssuesCountForUser(string userId)
        //{
        //    var result = await _issueService.GetOpenIssuesCountByUserAsync(userId);
        //    return ToActionResult(result);
        //}
    }
}
