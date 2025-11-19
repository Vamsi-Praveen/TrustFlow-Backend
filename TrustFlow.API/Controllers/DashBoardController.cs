using Microsoft.AspNetCore.Mvc;
using TrustFlow.Core.Services;

namespace TrustFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashBoardController : BaseController<DashBoardController>
    {
        private readonly UserService _userService;
        private readonly RolePermissionService _rolePermissionService;
        private readonly ProjectService _projectService;
        private readonly IssueService _issueService;
        private readonly LogService _logService;
        public DashBoardController(UserService userService,
            RolePermissionService rolePermissionService,
            ProjectService projectService,
            IssueService issueService,
            ILogger<DashBoardController> logger,
            LogService logService):base(logService,logger)
        {
            _userService = userService;
            _rolePermissionService = rolePermissionService;
            _projectService = projectService;
            _issueService = issueService;
            _logService = logService;
        }

        [HttpGet("GetDashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userResult = await _userService.GetUserCountAsync();
            var issueCountResult = await _issueService.GetIssuesCountAsync();
            var projectResult = await _projectService.GetProjectsCountAsync();
            var issueCreatedTodayResult = await _issueService.GetIssuesCreatedTodayCountAsync();
            if (!userResult.Success || !issueCountResult.Success || !projectResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An internal error occurred while retrieving dashboard statistics." });
            }
            var dashboardStats = new
            {
                totalUsers = (int)(long)userResult.Result,
                totalProjects = (int)(long)projectResult.Result,
                totalIssues = (int)(long)issueCountResult.Result,
                issuesCreatedToday = (int)(long)issueCreatedTodayResult.Result
            };
            return Ok(new { Success = true, Message = "Dashboard statistics retrieved successfully.", Result = dashboardStats });
        }

        [HttpGet("GetUserDashBoardStats")]
        public async Task<IActionResult> GetUserDashBoardStats()
        {
            var openIssuesResult = await _issueService.GetOpenIssuesCountByUserId(Id);
            var closedIssuesResult = await _issueService.GetFixedIssuesCountByUserId(Id);
            var projectCountResult = await _projectService.GetProjectsCountByUserId(Id);
            var recentIssuesResult = await _issueService.GetRecentIssuesCountByUserId(Id);
            if (!openIssuesResult.Success || !closedIssuesResult.Success ||
                !projectCountResult.Success || !recentIssuesResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An internal error occurred while retrieving user dashboard statistics." });
            }
            var userDashboardStats = new
            {
                myOpenIssues = (int)(long)openIssuesResult.Result,
                resolvedByMe = (int)(long)closedIssuesResult.Result,
                myProjects = (int)(long)projectCountResult.Result,
                recentActivity = (int)(long)recentIssuesResult.Result
            };
            return Ok(new
            {
                Success = true,
                Message = "User dashboard statistics retrieved successfully.",
                Result = userDashboardStats
            });
        }

        [HttpGet("GetUserOpenIssueList")]
        public async Task<IActionResult> GetUserOpenIssueList()
        {
            var openIssuesResult = await _issueService.GetUserOpenIssuesListAsync(Id);
            if (!openIssuesResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An internal error occurred while retrieving user open issues." });
            }
            return Ok(new
            {
                Success = true,
                Message = "User open issues retrieved successfully.",
                Result = openIssuesResult.Result
            });
        }

        [HttpGet("GetRecentActivityList")]
        public async Task<IActionResult> GetRecentActivityList()
        {
            var recentActivityResult = await _logService.GetRecentActivityListAsync(10);
            if (!recentActivityResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An internal error occurred while retrieving user recent activities." });
            }
            return Ok(new
            {
                Success = true,
                Message = "User recent activities retrieved successfully.",
                Result = recentActivityResult.Result
            });
        }

        [HttpGet("GetRoleOverview")]
        public async Task<IActionResult> GetRoleOverview()
        {
            var roleOverviewResult = await _rolePermissionService.GetRoleOverviewAsync();
            if (!roleOverviewResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An internal error occurred while retrieving role overview." });
            }
            return Ok(new
            {
                Success = true,
                Message = "Role overview retrieved successfully.",
                Result = roleOverviewResult.Result
            });
        }

        [HttpGet("GetUserRecentActivityList")]
        public async Task<IActionResult> GetUserRecentActivityList()
        {
            var userRecentActivityResult = await _logService.GetUserRecentActivityListAsync(Id);

            if (!userRecentActivityResult.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An internal error occurred while retrieving user recent activities." });
            }
            return Ok(new
            {
                Success = true,
                Message = "User recent activities retrieved successfully.",
                Result = userRecentActivityResult.Result
            });
        }
    }
}
