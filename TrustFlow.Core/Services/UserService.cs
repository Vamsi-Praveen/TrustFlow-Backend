using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.Helpers;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<RolePermission> _roles;
        private readonly PasswordHelper _passwordHelper;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationContext context, PasswordHelper passwordHelper, ILogger<UserService> logger)
        {
            _users = context.Users;
            _roles = context.RolePermissions;
            _passwordHelper = passwordHelper;
            _logger = logger;
        }

        public async Task<ServiceResult> GetUsersAsync()
        {
            try
            {
                var users = await _users.Find(_ => true).ToListAsync();
                return new ServiceResult(true, "Users retrieved successfully.", users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all users.");
                return new ServiceResult(false, "An internal error occurred while retrieving users.");
            }
        }

        public async Task<ServiceResult> GetUserByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return new ServiceResult(false, "User ID cannot be empty.");
                }
                var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ServiceResult(false, $"User with ID '{id}' not found.");
                }

                return new ServiceResult(true, "User retrieved successfully.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by ID: {UserId}", id);
                return new ServiceResult(false, "An internal error occurred while retrieving the user.");
            }
        }

        public async Task<ServiceResult> GetUserByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return new ServiceResult(false, "Username cannot be empty.");
                }

                var user = await _users.Find(u => u.Email.ToLower() == username.ToLower()).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ServiceResult(false, $"User with username '{username}' not found.");
                }

                return new ServiceResult(true, "User retrieved successfully.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by username: {Username}", username);
                return new ServiceResult(false, "An internal error occurred while retrieving the user.");
            }
        }

        public async Task<ServiceResult> CreateAsync(User newUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newUser.Email) || string.IsNullOrWhiteSpace(newUser.Username) || string.IsNullOrWhiteSpace(newUser.PasswordHash))
                {
                    _logger.LogWarning("Attempted to create user with missing required fields.");
                    return new ServiceResult(false, "Username, Email, and Password are required.");
                }

                var existing = await _users.Find(u => u.Email.ToLower() == newUser.Email.ToLower() || u.Username.ToLower() == newUser.Username.ToLower()).FirstOrDefaultAsync();
                if (existing != null)
                {
                    string errorMessage = existing.Username.ToLower() == newUser.Username.ToLower()
                        ? $"A user with the username '{newUser.Username}' already exists."
                        : $"A user with the email '{newUser.Email}' already exists.";
                    _logger.LogWarning(errorMessage);
                    return new ServiceResult(false, errorMessage);
                }

                newUser.PasswordHash = _passwordHelper.HashPassword(newUser.PasswordHash);
                newUser.CreatedAt = DateTime.UtcNow;
                newUser.UpdatedAt = DateTime.UtcNow;
                newUser.IsActive = true;

                await _users.InsertOneAsync(newUser);
                _logger.LogInformation("Successfully created new user: {Username}", newUser.Username);
                return new ServiceResult(true, "User created successfully.", newUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user: {Username}", newUser.Username);
                return new ServiceResult(false, "An internal error occurred while creating the user.");
            }
        }

        public async Task<ServiceResult> UpdateAsync(string id, User updatedUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return new ServiceResult(false, "User ID is required for update.");
                }

                var getResult = await GetUserByIdAsync(id);
                if (!getResult.Success)
                {
                    return getResult;
                }
                var existingUser = (User)getResult.Result;

                if (existingUser.Email.ToLower() != updatedUser.Email.ToLower())
                {
                    var duplicate = await _users.Find(u => u.Id != id && u.Email.ToLower() == updatedUser.Email.ToLower()).FirstOrDefaultAsync();
                    if (duplicate != null)
                    {
                        return new ServiceResult(false, $"The email '{updatedUser.Email}' is already in use by another account.");
                    }
                }

                existingUser.Email = updatedUser.Email;
                existingUser.FirstName = updatedUser.FirstName;
                existingUser.LastName = updatedUser.LastName;
                existingUser.IsActive = updatedUser.IsActive;
                existingUser.Role = updatedUser.Role;
                existingUser.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(updatedUser.PasswordHash))
                {
                    existingUser.PasswordHash = _passwordHelper.HashPassword(updatedUser.PasswordHash);
                    _logger.LogInformation("Password updated for user: {Username}", existingUser.Username);
                }

                var result = await _users.ReplaceOneAsync(u => u.Id == id, existingUser);
                if (result.IsAcknowledged && result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Successfully updated user: {UserId}", id);
                    return new ServiceResult(true, "User updated successfully.", existingUser);
                }

                return new ServiceResult(false, "No changes were detected for the user.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user with ID: {UserId}", id);
                return new ServiceResult(false, "An internal error occurred while updating the user.");
            }
        }

        public async Task<ServiceResult> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return new ServiceResult(false, "User ID cannot be empty.");
                }

                var result = await _users.DeleteOneAsync(u => u.Id == id);
                if (result.IsAcknowledged && result.DeletedCount > 0)
                {
                    _logger.LogInformation("Successfully deleted user with ID: {UserId}", id);
                    return new ServiceResult(true, "User deleted successfully.");
                }

                return new ServiceResult(false, $"User with ID '{id}' not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID: {UserId}", id);
                return new ServiceResult(false, "An internal error occurred while deleting the user.");
            }
        }

        public async Task<ServiceResult> AuthenticateAsync(string username, string password)
        {
            try
            {
                var getResult = await GetUserByUsernameAsync(username);
                if (!getResult.Success)
                    return new ServiceResult(false, "Invalid username or password.");

                var user = (User)getResult.Result;

                if (!user.IsActive)
                    return new ServiceResult(false, "Your account is inactive.");

                if (!_passwordHelper.VerifyPassword(password, user.PasswordHash))
                    return new ServiceResult(false, "Invalid username or password.");

                return new ServiceResult(true, "Authentication successful.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for user: {Username}", username);
                return new ServiceResult(false, "An internal error occurred during authentication.");
            }
        }

    }
}
