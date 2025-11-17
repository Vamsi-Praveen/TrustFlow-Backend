using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class ProjectService
    {
        private readonly IMongoCollection<Project> _projects;
        private readonly IMongoCollection<User> _users;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ApplicationContext context, ILogger<ProjectService> logger)
        {
            _projects = context.Projects;
            _users = context.Users;
            _logger = logger;
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
                {
                    return new ServiceResult(false, "Project ID is required for update.");
                }

                var getResult = await GetProjectByIdAsync(id);
                if (!getResult.Success)
                {
                    return getResult;
                }
                var existingProject = (Project)getResult.Result;

                if (string.IsNullOrWhiteSpace(updatedProject.Name))
                {
                    return new ServiceResult(false, "Project name cannot be empty.");
                }

                if (existingProject.Name.ToLower() != updatedProject.Name.ToLower())
                {
                    var duplicateNameProject = await _projects.Find(p => p.Id != id && p.Name.ToLower() == updatedProject.Name.ToLower()).FirstOrDefaultAsync();
                    if (duplicateNameProject != null)
                    {
                        return new ServiceResult(false, $"A project with the name '{updatedProject.Name}' already exists.");
                    }
                }

                if (existingProject.LeadUserId != updatedProject.LeadUserId && !string.IsNullOrWhiteSpace(updatedProject.LeadUserId))
                {
                    var leadUser = await _users.Find(u => u.Id == updatedProject.LeadUserId).FirstOrDefaultAsync();
                    if (leadUser == null)
                    {
                        return new ServiceResult(false, $"The specified new Lead User with ID '{updatedProject.LeadUserId}' was not found.");
                    }
                }

                if (existingProject.ManagerUserId != updatedProject.ManagerUserId && !string.IsNullOrWhiteSpace(updatedProject.ManagerUserId))
                {
                    var manager = await _users.Find(u => u.Id == updatedProject.ManagerUserId).FirstOrDefaultAsync();
                    if (manager == null)
                    {
                        return new ServiceResult(false, $"The specified new Manager User with ID '{updatedProject.ManagerUserId}' was not found.");
                    }
                }

                updatedProject.Id = id;
                updatedProject.CreatedAt = existingProject.CreatedAt;
                updatedProject.UpdatedAt = DateTime.UtcNow;
                updatedProject.Members ??= new List<ProjectMember>();


                var result = await _projects.ReplaceOneAsync(p => p.Id == id, updatedProject);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Successfully updated project with ID: {ProjectId}", id);
                    return new ServiceResult(true, "Project updated successfully.", updatedProject);
                }

                return new ServiceResult(false, "No changes were detected for the project.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update project with ID: {ProjectId}", id);
                return new ServiceResult(false, "An internal error occurred while updating the project.");
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

                var userExists = await _users.Find(u => u.Id == newMember.UserId).AnyAsync();
                if (!userExists)
                {
                    return new ServiceResult(false, $"User with ID '{newMember.UserId}' does not exist.");
                }

                var project = (Project)projectResult.Result;
                if (project.Members.Any(m => m.UserId == newMember.UserId))
                {
                    return new ServiceResult(false, $"User with ID '{newMember.UserId}' is already a member of this project.");
                }

                var filter = Builders<Project>.Filter.Eq(p => p.Id, projectId);
                var update = Builders<Project>.Update
                    .AddToSet(p => p.Members, newMember)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);

                var result = await _projects.UpdateOneAsync(filter, update);

                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
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
    }
}