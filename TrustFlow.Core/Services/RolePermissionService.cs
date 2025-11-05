using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class RolePermissionService
    {
        private readonly IMongoCollection<RolePermission> _roles;
        private readonly ILogger<RolePermissionService> _logger;

        public RolePermissionService(ApplicationContext context, ILogger<RolePermissionService> logger)
        {
            _logger = logger;
            _roles = context.RolePermissions;
        }

        public async Task<ServiceResult> GetRolePermissionsAsync()
        {
            try
            {
                var roles = await _roles.Find(_ => true).ToListAsync();
                return new ServiceResult(true, "Roles retrieved successfully.", roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all roles.");
                return new ServiceResult(false, "An internal error occurred while retrieving roles.");
            }
        }

        public async Task<ServiceResult> GetRolePermissionByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Attempted to retrieve role with null or empty ID.");
                    return new ServiceResult(false, "Role ID cannot be empty.");
                }

                var role = await _roles.Find(p => p.Id == id).FirstOrDefaultAsync();

                if (role == null)
                {
                    _logger.LogWarning("Role with ID: {RoleId} not found.", id);
                    return new ServiceResult(false, $"Role with ID '{id}' not found.");
                }
                return new ServiceResult(true, "Role retrieved successfully.", role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve role by ID: {RoleId}", id);
                return new ServiceResult(false, "An internal error occurred while retrieving the role.");
            }
        }

        public async Task<ServiceResult> CreateRolePermissionAsync(RolePermission newRolePermission)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newRolePermission.RoleName))
                {
                    _logger.LogWarning("Attempted to create a role with an empty name.");
                    return new ServiceResult(false, "Role name is required.");
                }

                var existingRole = await _roles.Find(r => r.RoleName.ToLower() == newRolePermission.RoleName.ToLower()).FirstOrDefaultAsync();
                if (existingRole != null)
                {
                    _logger.LogWarning("Attempted to create a role with a duplicate name: {RoleName}", newRolePermission.RoleName);
                    return new ServiceResult(false, $"A role with the name '{newRolePermission.RoleName}' already exists.");
                }

                newRolePermission.CreatedAt = DateTime.UtcNow;
                newRolePermission.UpdatedAt = DateTime.UtcNow;

                await _roles.InsertOneAsync(newRolePermission);
                _logger.LogInformation("Successfully created new role: {RoleName} with ID: {RoleId}", newRolePermission.RoleName, newRolePermission.Id);
                return new ServiceResult(true, "Role created successfully.", newRolePermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create role: {RoleName}", newRolePermission.RoleName);
                return new ServiceResult(false, "An internal error occurred while creating the role.");
            }
        }

        public async Task<ServiceResult> UpdateRolePermissionAsync(string id, RolePermission updatedRolePermission)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Attempted to update a role with a null or empty ID.");
                    return new ServiceResult(false, "Role ID is required for update.");
                }

                var getResult = await GetRolePermissionByIdAsync(id);
                if (!getResult.Success)
                {
                    return getResult;
                }
                var existingRole = (RolePermission)getResult.Result;

                if (existingRole.RoleName.ToLower() != updatedRolePermission.RoleName.ToLower())
                {
                    var duplicateRole = await _roles.Find(r => r.Id != id && r.RoleName.ToLower() == updatedRolePermission.RoleName.ToLower()).FirstOrDefaultAsync();
                    if (duplicateRole != null)
                    {
                        _logger.LogWarning("Attempted to update role {RoleId} to a duplicate name: {RoleName}", id, updatedRolePermission.RoleName);
                        return new ServiceResult(false, $"A role with the name '{updatedRolePermission.RoleName}' already exists.");
                    }
                }

                updatedRolePermission.Id = id;
                updatedRolePermission.CreatedAt = existingRole.CreatedAt;
                updatedRolePermission.UpdatedAt = DateTime.UtcNow;

                var result = await _roles.ReplaceOneAsync(r => r.Id == id, updatedRolePermission);

                if (!result.IsAcknowledged)
                {
                    _logger.LogError("Role update for ID {RoleId} was not acknowledged by the database.", id);
                    return new ServiceResult(false, "Database did not acknowledge the update operation.");
                }

                if (result.ModifiedCount == 0)
                {
                    _logger.LogWarning("Role update for ID {RoleId} resulted in no changes.", id);
                    return new ServiceResult(false, "No changes were detected for the role.");
                }

                _logger.LogInformation("Successfully updated role with ID: {RoleId}", id);
                return new ServiceResult(true, "Role updated successfully.", updatedRolePermission);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update role with ID: {RoleId}", id);
                return new ServiceResult(false, "An internal error occurred while updating the role.");
            }
        }

        public async Task<ServiceResult> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("Attempted to delete role with null or empty ID.");
                    return new ServiceResult(false, "Role ID cannot be empty.");
                }

                var result = await _roles.DeleteOneAsync(p => p.Id == id);
                if (result.IsAcknowledged && result.DeletedCount > 0)
                {
                    _logger.LogInformation("Successfully deleted role with ID: {roleid}", id);
                    return new ServiceResult(true, "Role deleted successfully.");
                }

                _logger.LogWarning("Role deletion for ID: {roleid} failed. The role may not exist.", id);
                return new ServiceResult(false, $"Role with ID '{id}' not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete role with ID: {roleId}", id);
                return new ServiceResult(false, "An internal error occurred while deleting the role.");
            }
        }
    }
}