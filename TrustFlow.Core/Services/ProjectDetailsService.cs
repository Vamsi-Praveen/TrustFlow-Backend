using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class ProjectDetailsService
    {
        public readonly IMongoCollection<Project> _projects;
        public readonly IMongoCollection<Issue> _issues;
        public readonly IMongoCollection<User> _users;
        public readonly ILogger<ProjectDetailsService> _logger;
        public readonly IMongoCollection<IssueStatus> _issueStatus;
        public readonly IMongoCollection<IssueType> _issueType;
        public readonly UserService _userService;


        public ProjectDetailsService(ApplicationContext context, ILogger<ProjectDetailsService> logger, UserService userService)
        {
            _projects = context.Projects;
            _issues = context.Issues;
            _users = context.Users;
            _logger = logger;
            _issueStatus = context.IssueStatus;
            _issueType = context.IssueTypes;
            _userService = userService;
        }

        public async Task<ServiceResult> GetProjectOverview(string id)
        {
            try
            {
                var exisitngProject = await _projects.Find(x => x.Id == id).FirstOrDefaultAsync();

                if (exisitngProject == null)
                {
                    _logger.LogInformation($"Project not found with id {id}");
                }

                var usersResult = await _userService.GetUsersAsync();


                var projectIssues = await _issues.Find(x => x.ProjectId == id).ToListAsync();

                if (!usersResult.Success)
                {
                    _logger.LogInformation($"Unable to fetch the users");
                    return new ServiceResult(false, "Unable to fetch the users");
                }
                var users = (List<User>)usersResult.Result;

                if (users.Count == 0)
                {
                    _logger.LogInformation($"No users found");
                    return new ServiceResult(false, "No Users Found");
                }
                var userDict = users.ToDictionary(
                    u => u.Id.ToString(),
                    u => new
                    {
                        u.FullName,
                        u.Email,
                        u.ProfilePictureUrl
                    }
                );

                var issueStatus = Builders<IssueStatus>.Filter.Eq(s => s.Name, "Open");
                var issueType = Builders<IssueType>.Filter.Eq(s => s.Name, "Bug");

                var status = await _issueStatus.Find(issueStatus).FirstOrDefaultAsync();

                var type = await _issueType.Find(issueType).FirstOrDefaultAsync();

                ProjectDetailsOverview projectDetailsOverview = new ProjectDetailsOverview();

                foreach (var member in exisitngProject.Members)
                {
                    var user = userDict[member.UserId.ToString()];
                  
                    projectDetailsOverview.Members.Add(new ProjectMembers
                    {
                        MemberName = user.FullName,
                        MemberProfileImage = user.ProfilePictureUrl,
                        MemberEmail = user.Email,
                        MemberAssignedIssues = projectIssues.Count(x => x.AssigneeUserIds.Contains(member.UserId.ToString()))
                    });
                }

                projectDetailsOverview.ProjectStats = new ProjectStats
                {
                    TotalIssues = projectIssues.Count(),
                    OpenIssues = projectIssues.Count(x => x.Status == status.Id),
                    Bugs = projectIssues.Count(x=>x.Type == type.Id.ToString())
                };

                return new ServiceResult(true, "Fetched Project Dashboard Details", projectDetailsOverview);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to  get project overiew for project id :{id}", ex);
                return new ServiceResult(false, "An internal error occurred while retrieving projects details.");
            }
        }
    }
}
