using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class ProjectService : BaseService<ProjectService>
    {
        private readonly IMongoCollection<Project> _projects;
        private readonly IMongoCollection<User> _users;

        public ProjectService(ApplicationContext context, ILogger<ProjectService> logger, LogService logService, UserContextService contextService) : base(logService, logger, contextService)
        {
            _projects = context.Projects;
            _users = context.Users;
        }

        public async Task<ServiceResult> GetProjectsAsync()
        {
            try
            {
                var projects = await _projects.Find(_ => true).ToListAsync();
                return new ServiceResult(true, "Projects retrieved successfully.", projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all projects.");
                return new ServiceResult(false, "An internal error occurred while retrieving projects.");
            }
        }

        public async Task<ServiceResult> GetProjectByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Attempted to retrieve project with null or empty ID.");
                    return new ServiceResult(false, "Project ID cannot be empty.");
                }

                var project = await _projects.Find(p => p.Id == id).FirstOrDefaultAsync();

                if (project == null)
                {
                    _logger.LogWarning("Project with ID {ProjectId} not found.", id);
                    return new ServiceResult(false, $"Project with ID '{id}' not found.");
                }

                return new ServiceResult(true, "Project retrieved successfully.", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve project by ID: {ProjectId}", id);
                return new ServiceResult(false, "An internal error occurred while retrieving the project.");
            }
        }

        public async Task<ServiceResult> GetProjectsForUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Attempted to retrieve project with null or empty userid.");
                    return new ServiceResult(false, "User id cannot be empty.");
                }

                var isLeadFilter = Builders<Project>.Filter.Eq(p => p.LeadUserId, userId);

                var isMemberFilter = Builders<Project>.Filter.ElemMatch(p => p.Members, m => m.UserId == userId);

                var combinedFilter = Builders<Project>.Filter.Or(isLeadFilter, isMemberFilter);

                var projects = await _projects.Find(combinedFilter).ToListAsync();

                _logger.LogInformation("Retrieved {Count} projects for user {UserId}.", projects.Count, userId);
                return new ServiceResult(true, $"Projects for user '{userId}' retrieved successfully.", projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve project for user ID: {userid}", userId);
                return new ServiceResult(false, "An internal error occurred while retrieving the project for user.");
            }
        }

        public async Task<ServiceResult> CreateAsync(Project newProject)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newProject.Name))
                {
                    _logger.LogWarning("Attempted to create project with missing name.");
                    return new ServiceResult(false, "Project name is required.");
                }

                var existingProject = await _projects.Find(p => p.Name.ToLower() == newProject.Name.ToLower()).FirstOrDefaultAsync();
                if (existingProject != null)
                {
                    _logger.LogWarning("Attempted to create project with duplicate name: {ProjectName}", newProject.Name);
                    return new ServiceResult(false, $"A project with the name '{newProject.Name}' already exists.");
                }

                if (!string.IsNullOrWhiteSpace(newProject.LeadUserId))
                {
                    var leadUser = await _users.Find(u => u.Id == newProject.LeadUserId).FirstOrDefaultAsync();
                    newProject.LeadProfilePicUrl = leadUser.ProfilePictureUrl;
                    if (leadUser == null)
                    {
                        _logger.LogWarning("Attempted to create project with invalid LeadUserId: {LeadUserId}", newProject.LeadUserId);
                        return new ServiceResult(false, $"The specified Lead User with ID '{newProject.LeadUserId}' was not found.");
                    }
                }
                else
                {
                    _logger.LogWarning("Attempted to create project without a specified LeadUserId.");
                }

                if (!string.IsNullOrWhiteSpace(newProject.ManagerUserId))
                {
                    var manager = await _users.Find(u => u.Id == newProject.ManagerUserId).FirstOrDefaultAsync();
                    newProject.ManagerProfilePicUrl = manager.ProfilePictureUrl;
                    if (manager == null)
                    {
                        _logger.LogWarning("Attempted to create project with invalid ManagerUserId: {ManagerUserId}", newProject.ManagerUserId);
                        return new ServiceResult(false, $"The specified Manager User with ID '{newProject.ManagerUserId}' was not found.");
                    }
                }
                else
                {
                    _logger.LogWarning("Attempted to create project without a specified ManagerUserID.");
                }

                newProject.CreatedAt = DateTime.UtcNow;
                newProject.UpdatedAt = DateTime.UtcNow;
                newProject.Members ??= new List<ProjectMember>();

                await _projects.InsertOneAsync(newProject);

                var activityLog = new ActivityLog()
                {
                    Action = "Created",
                    Category = "Project",
                    ProjectId = newProject.Id,
                    Description = $"Project {newProject.Name} is created",
                    Status = "Success",
                    EntityType = "Project",
                    UserId = _userContextService.UserId,
                    IpAddress = _userContextService.IpAddress,
                    UserAgent = _userContextService.UserAgent
                };

                await SendLogAsync(activityLog);

                _logger.LogInformation("Successfully created new project: {ProjectName}", newProject.Name);
                return new ServiceResult(true, "Project created successfully.", newProject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create project: {ProjectName}", newProject.Name);
                return new ServiceResult(false, "An internal error occurred while creating the project.");
            }
        }

        public async Task<ServiceResult> UpdateAsync(string id, Project updatedProject)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return new ServiceResult(false, "Project ID is required for update.");

                var getResult = await GetProjectByIdAsync(id);
                if (!getResult.Success)
                    return getResult;

                var existingProject = (Project)getResult.Result;

                if (string.IsNullOrWhiteSpace(updatedProject.Name))
                    return new ServiceResult(false, "Project name cannot be empty.");

                if (existingProject.Name.ToLower() != updatedProject.Name.ToLower())
                {
                    var duplicateName = await _projects
                        .Find(p => p.Id != id && p.Name.ToLower() == updatedProject.Name.ToLower())
                        .FirstOrDefaultAsync();

                    if (duplicateName != null)
                        return new ServiceResult(false, $"A project named '{updatedProject.Name}' already exists.");
                }

                if (existingProject.LeadUserId != updatedProject.LeadUserId &&
                    !string.IsNullOrWhiteSpace(updatedProject.LeadUserId))
                {
                    var leadUser = await _users.Find(u => u.Id == updatedProject.LeadUserId).FirstOrDefaultAsync();
                    updatedProject.LeadUserName = leadUser.Username;
                    updatedProject.LeadProfilePicUrl = leadUser.ProfilePictureUrl;
                    if (leadUser == null)
                        return new ServiceResult(false, $"Lead User ID '{updatedProject.LeadUserId}' not found.");
                }

                if (existingProject.ManagerUserId != updatedProject.ManagerUserId &&
                    !string.IsNullOrWhiteSpace(updatedProject.ManagerUserId))
                {
                    var manager = await _users.Find(u => u.Id == updatedProject.ManagerUserId).FirstOrDefaultAsync();
                    updatedProject.ManagerUserName = manager.Username;
                    updatedProject.ManagerProfilePicUrl = manager.ProfilePictureUrl;
                    if (manager == null)
                        return new ServiceResult(false, $"Manager User ID '{updatedProject.ManagerUserId}' not found.");
                }

                var updateDef = Builders<Project>.Update
                    .Set(p => p.Name, updatedProject.Name)
                    .Set(p => p.Description, updatedProject.Description)
                    .Set(p => p.LeadUserId, updatedProject.LeadUserId)
                    .Set(p => p.LeadUserName, updatedProject.LeadUserName)
                    .Set(p => p.LeadProfilePicUrl, updatedProject.LeadProfilePicUrl)
                    .Set(p => p.ManagerUserId, updatedProject.ManagerUserId)
                    .Set(p => p.ManagerUserName, updatedProject.ManagerUserName)
                    .Set(p => p.ManagerProfilePicUrl, updatedProject.ManagerProfilePicUrl)
                    .Set(p => p.Members, updatedProject.Members ?? existingProject.Members)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);

                var result = await _projects.UpdateOneAsync(
                    p => p.Id == id,
                    updateDef
                );

                if (result.ModifiedCount > 0)
                {
                    var activityLog = new ActivityLog()
                    {
                        Action = "Updated",
                        Category = "Project",
                        ProjectId = id,
                        Description = $"Project '{updatedProject.Name}' updated successfully",
                        EntityType = "Project",
                        Status = "Success",
                        UserId = _userContextService.UserId,
                        IpAddress = _userContextService.IpAddress,
                        UserAgent = _userContextService.UserAgent
                    };

                    await SendLogAsync(activityLog);

                    _logger.LogInformation("Project updated: {ProjectId}", id);

                    return new ServiceResult(true, "Project updated successfully.", updatedProject);
                }

                return new ServiceResult(false, "No changes detected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update project ID: {ProjectId}", id);
                return new ServiceResult(false, "An error occurred while updating the project.");
            }
        }


        public async Task<ServiceResult> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return new ServiceResult(false, "Project ID cannot be empty.");
                }

                var result = await _projects.DeleteOneAsync(p => p.Id == id);
                if (result.IsAcknowledged && result.DeletedCount > 0)
                {
                    var activityLog = new ActivityLog()
                    {
                        Action = "Deleted",
                        Category = "Project",
                        ProjectId = id,
                        Description = $"Project with id {id} is deleted",
                        Status = "Success",
                        EntityType = "Project",
                        UserId = _userContextService.UserId,
                        IpAddress = _userContextService.IpAddress,
                        UserAgent = _userContextService.UserAgent
                    };

                    await SendLogAsync(activityLog);
                    _logger.LogInformation("Successfully deleted project with ID: {ProjectId}", id);
                    return new ServiceResult(true, "Project deleted successfully.");
                }

                return new ServiceResult(false, $"Project with ID '{id}' not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete project with ID: {ProjectId}", id);
                return new ServiceResult(false, "An internal error occurred while deleting the project.");
            }
        }

        public async Task<ServiceResult> AddMemberToProjectAsync(string projectId, ProjectMember newMember)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectId) || newMember == null || string.IsNullOrWhiteSpace(newMember.UserId))
                {
                    return new ServiceResult(false, "Project ID and valid member details are required.");
                }

                var projectResult = await GetProjectByIdAsync(projectId);
                if (!projectResult.Success)
                {
                    return new ServiceResult(false, $"Project with ID '{projectId}' not found.");
                }

                var userExists = await _users.Find(u => u.Id == newMember.UserId).FirstOrDefaultAsync();
                if (userExists == null)
                {
                    return new ServiceResult(false, $"User with ID '{newMember.UserId}' does not exist.");
                }

                var project = (Project)projectResult.Result;
                if (project.Members.Any(m => m.UserId == newMember.UserId))
                {
                    return new ServiceResult(false, $"User with ID '{newMember.UserId}' is already a member of this project.");
                }
                newMember.ProfilePicUrl = userExists.ProfilePictureUrl;

                var filter = Builders<Project>.Filter.Eq(p => p.Id, projectId);
                var update = Builders<Project>.Update
                    .AddToSet(p => p.Members, newMember)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);

                var result = await _projects.UpdateOneAsync(filter, update);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    var activityLog = new ActivityLog()
                    {
                        Action = "Added",
                        Category = "Project",
                        ProjectId = projectId,
                        Description = $"User {newMember.UserName} is added to Project {project.Name}",
                        Status = "Success",
                        EntityType = "Project",
                        UserId = _userContextService.UserId,
                        IpAddress = _userContextService.IpAddress,
                        UserAgent = _userContextService.UserAgent
                    };

                    await SendLogAsync(activityLog);
                    _logger.LogInformation("Successfully added user {MemberUserId} to project {ProjectId}.", newMember.UserId, projectId);
                    return new ServiceResult(true, "Member added successfully.");
                }

                return new ServiceResult(false, "Failed to add member to the project.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member {MemberUserId} to project {ProjectId}.", newMember?.UserId, projectId);
                return new ServiceResult(false, "An internal error occurred while adding a member.");
            }
        }

        public async Task<ServiceResult> RemoveMemberFromProjectAsync(string projectId, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(userId))
                {
                    return new ServiceResult(false, "Project ID and User ID are required.");
                }

                var filter = Builders<Project>.Filter.Eq(p => p.Id, projectId);
                var update = Builders<Project>.Update
                    .PullFilter(p => p.Members, m => m.UserId == userId)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);

                var result = await _projects.UpdateOneAsync(filter, update);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    var activityLog = new ActivityLog()
                    {
                        Action = "Removed",
                        Category = "Project",
                        ProjectId = projectId,
                        Description = $"User with {userId} is deleted from Project {projectId}",
                        Status = "Success",
                        EntityType = "Project",
                        UserId = _userContextService.UserId,
                        IpAddress = _userContextService.IpAddress,
                        UserAgent = _userContextService.UserAgent
                    };

                    await SendLogAsync(activityLog);
                    _logger.LogInformation("Successfully removed user {UserId} from project {ProjectId}.", userId, projectId);
                    return new ServiceResult(true, "Member removed successfully.");
                }

                return new ServiceResult(false, "Failed to remove member. They may not be a member of this project.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {UserId} from project {ProjectId}.", userId, projectId);
                return new ServiceResult(false, "An internal error occurred while removing a member.");
            }
        }

        public async Task<ServiceResult> GetProjectsCountAsync()
        {
            try
            {
                var count = await _projects.CountDocumentsAsync(_ => true);
                return new ServiceResult(true, "Projects count retrieved successfully.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve projects count.");
                return new ServiceResult(false, "An internal error occurred while retrieving projects count.");
            }
        }

        public async Task<ServiceResult> GetProjectsCountByUserId(string userId)
        {
            try
            {
                var filter = Builders<Project>.Filter.Or(
                Builders<Project>.Filter.Eq(p => p.LeadUserId, userId),
                Builders<Project>.Filter.ElemMatch(p => p.Members, m => m.UserId == userId)
                );

                var count = await _projects.CountDocumentsAsync(filter);
                return new ServiceResult(true, "User's projects count retrieved successfully.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve projects count for user ID: {UserId}", userId);
                return new ServiceResult(false, "An internal error occurred while retrieving user's projects count.");
            }
        }

        public async Task<ServiceResult> IsUserExistInProject(string userid)
        {
            try
            {
                var filter = Builders<Project>.Filter.Or(
                    Builders<Project>.Filter.Eq(p => p.LeadUserId, userid),
                    Builders<Project>.Filter.Eq(p => p.ManagerUserId, userid),
                    Builders<Project>.Filter.ElemMatch(
                        p => p.Members,
                        m => m.UserId == userid
                    )
                );
                var isUserExistInProject = await _projects.Find(filter).AnyAsync();
                
                return new ServiceResult(true,"Succesfully Verfied user exist in project",isUserExistInProject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch user exists in  projects for user ID: {UserId}", userid);
                return new ServiceResult(false, "An internal error occurred while fetch user exists in  projects.",null);
            }
        }
    }
}