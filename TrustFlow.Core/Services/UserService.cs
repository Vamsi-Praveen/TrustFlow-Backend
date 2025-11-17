using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Helpers;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<RolePermission> _roles;
        private readonly IMongoCollection<UserNotificationSetting> _userNotificationSettings;
        private readonly PasswordHelper _passwordHelper;
        private readonly ILogger<UserService> _logger;
        private readonly RedisCacheService _redisCacheService;

        public UserService(ApplicationContext context, PasswordHelper passwordHelper, ILogger<UserService> logger, RedisCacheService redisCacheService)
        {
            _users = context.Users;
            _roles = context.RolePermissions;
            _userNotificationSettings = context.UserNotificationSettings;
            _passwordHelper = passwordHelper;
            _logger = logger;
            _redisCacheService = redisCacheService;
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

        public async Task<ServiceResult> GetCompleteUserById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogInformation("User id cannot be empty");
                    return new ServiceResult(false, "User ID cannot be empty.");
                }

                var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogInformation($"User with ID '{id}' not found.");
                    return new ServiceResult(false, $"User with ID '{id}' not found.");
                }

                var role = await _roles.Find(r => r.Id == user.RoleId).FirstOrDefaultAsync();

                if (role == null)
                {
                    _logger.LogError($"Role not found id {user.RoleId}");
                }

                //Filter only allowed role permissions
                var rolePermissions = role?
                          .GetType()
                          .GetProperties()
                          .Where(p => p.PropertyType == typeof(bool) && (bool)p.GetValue(role) == true)
                          .Select(p => p.Name)
                          .ToList();

                _logger.LogInformation($"User {user.Id} roles: {rolePermissions} ");

                var userResponse = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.RoleId,
                    user.Role,
                    user.IsActive,
                    user.CreatedAt,
                    user.UpdatedAt,
                    Permissions = rolePermissions,
                    user.DefaultPasswordChanged,
                    user.PhoneNumber,
                    user.ProfilePictureUrl
                };

                return new ServiceResult(true, "User retrieved successfully.", userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user by ID: {UserId}", id);
                return new ServiceResult(false, "An internal error occurred while retrieving the user.");
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
                var existing = await _users.Find(u => u.Email.ToLower() == newUser.Email.ToLower() || u.Username.ToLower() == newUser.Username.ToLower()).FirstOrDefaultAsync();
                if (existing != null)
                {
                    string errorMessage = existing.Username.ToLower() == newUser.Username.ToLower()
                        ? $"A user with the username '{newUser.Username}' already exists."
                        : $"A user with the email '{newUser.Email}' already exists.";
                    _logger.LogWarning(errorMessage);
                    return new ServiceResult(false, errorMessage);
                }

                var roleId = newUser.RoleId;

                var isRoleExist = await _roles.Find(r => r.Id == roleId).FirstOrDefaultAsync();

                if (isRoleExist == null)
                {
                    _logger.LogError($"Role not found with id {roleId}");
                    return new ServiceResult(false, "Role not found");
                }



                newUser.FullName = $"{newUser.FirstName} {newUser.LastName}";
                newUser.Username = newUser.Username;
                newUser.PasswordHash = _passwordHelper.HashPassword("trustflow");
                newUser.CreatedAt = DateTime.UtcNow;
                newUser.UpdatedAt = DateTime.UtcNow;
                newUser.IsActive = true;
                await _users.InsertOneAsync(newUser);

                var userID = newUser.Id;
                var notificationConfig = new UserNotificationSetting()
                {
                    UserId = userID,
                    DefaultNotificationMethod = "Email",
                    NotifyOnAssignedBug = true,
                    NotifyOnStatusChange = true,
                    NotifyOnNewComment = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                await _userNotificationSettings.InsertOneAsync(notificationConfig);

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
                existingUser.Username = updatedUser.Username;
                existingUser.IsActive = updatedUser.IsActive;
                existingUser.Role = updatedUser.Role;
                existingUser.RoleId = updatedUser.RoleId;
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

        public async Task<ServiceResult> UpdateProfileAsync(string id, UpdateProfileDTO updatedUser)
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

                if (!string.Equals(existingUser.Email, updatedUser.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var duplicate = await _users
                        .Find(u => u.Id != id && u.Email.ToLower() == updatedUser.Email.ToLower())
                        .FirstOrDefaultAsync();

                    if (duplicate != null)
                    {
                        return new ServiceResult(false, $"The email '{updatedUser.Email}' is already in use.");
                    }
                }

                var update = Builders<User>.Update
                    .Set(u => u.Email, updatedUser.Email)
                    .Set(u => u.FirstName, updatedUser.FirstName)
                    .Set(u => u.LastName, updatedUser.LastName)
                    .Set(u => u.Username, updatedUser.UserName)
                    .Set(u => u.PhoneNumber, updatedUser.PhoneNumber)
                    .Set(u => u.ProfilePictureUrl, updatedUser.ProfilePicUrl)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                var result = await _users.UpdateOneAsync(u => u.Id == id, update);

                if (result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Successfully updated profile for user: {UserId}", id);
                    return new ServiceResult(true, "Profile updated successfully.");
                }

                return new ServiceResult(false, "No changes detected, profile remains the same.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile: {UserId}", id);
                return new ServiceResult(false, "An internal error occurred while updating the profile.");
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

        public async Task<ServiceResult> InitialSetPasswordAsync(string userId, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (!user.Success)
                {
                    return new ServiceResult(false, "User not found.");
                }

                var existingUser = (User)user.Result;
                existingUser.PasswordHash = _passwordHelper.HashPassword(newPassword);
                existingUser.DefaultPasswordChanged = true;
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _users.FindOneAndUpdateAsync(userId => userId.Id == existingUser.Id,
                    Builders<User>.Update
                    .Set(u => u.PasswordHash, existingUser.PasswordHash)
                    .Set(u => u.DefaultPasswordChanged, existingUser.DefaultPasswordChanged)
                    .Set(u => u.UpdatedAt, existingUser.UpdatedAt)
                );

                return new ServiceResult(true, "Initial password set successfully.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set initial password for user ID: {UserId}", userId);
                return new ServiceResult(false, "An internal error occurred while setting the initial password.");
            }
        }

        public async Task<ServiceResult> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (!user.Success)
                {
                    return new ServiceResult(false, "User not found.");
                }
                var existingUser = (User)user.Result;
                if (!_passwordHelper.VerifyPassword(oldPassword, existingUser.PasswordHash))
                {
                    return new ServiceResult(false, "Old password is incorrect.");
                }

                existingUser.PasswordHash = _passwordHelper.HashPassword(newPassword);

                existingUser.UpdatedAt = DateTime.UtcNow;

                await _users.FindOneAndUpdateAsync(userId => userId.Id == existingUser.Id,
                    Builders<User>.Update
                    .Set(u => u.PasswordHash, existingUser.PasswordHash)
                    .Set(u => u.UpdatedAt, existingUser.UpdatedAt)
                );

                return new ServiceResult(true, "Password changed successfully.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password for user ID: {UserId}", userId);
                return new ServiceResult(false, "An internal error occurred while changing the password.");
            }
        }

        public async Task<ServiceResult> UpdateUserNotificationConfig(string userId, UserNotification userNotificationSetting)
        {
            try
            {

                var user = await GetUserByIdAsync(userId);
                if (!user.Success)
                {
                    return new ServiceResult(false, "User not found.");
                }
                var existingSetting = await _userNotificationSettings.Find(u => u.UserId == userId).FirstOrDefaultAsync();

                if (existingSetting == null)
                {
                    _logger.LogInformation("User Config not found for user ID: {UserId}", userId);
                    return new ServiceResult(false, "User Config not found.");
                }

                existingSetting.DefaultNotificationMethod = userNotificationSetting.DefaultNotificationMethod;
                existingSetting.NotifyOnAssignedBug = userNotificationSetting.NotifyOnAssignedBug;
                existingSetting.NotifyOnStatusChange = userNotificationSetting.NotifyOnStatusChange;
                existingSetting.NotifyOnNewComment = userNotificationSetting.NotifyOnNewComment;
                existingSetting.UpdatedAt = DateTime.UtcNow;

                await _userNotificationSettings.FindOneAndUpdateAsync(setting => setting.Id == existingSetting.Id,
                    Builders<UserNotificationSetting>.Update
                    .Set(u => u.DefaultNotificationMethod, existingSetting.DefaultNotificationMethod)
                    .Set(u => u.NotifyOnAssignedBug, existingSetting.NotifyOnAssignedBug)
                    .Set(u => u.NotifyOnStatusChange, existingSetting.NotifyOnStatusChange)
                    .Set(u => u.NotifyOnNewComment, existingSetting.NotifyOnNewComment)
                    .Set(u => u.UpdatedAt, existingSetting.UpdatedAt)
                );

                return new ServiceResult(true, "User notification settings updated successfully.", existingSetting);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update notification settings for user ID: {UserId}", userId);
                return new ServiceResult(false, "An internal error occurred while updating notification settings.");
            }

        }

        public async Task<ServiceResult> GetUserNotificationConfig(string userId)
        {
            try
            {
                var notificationConfig = await _userNotificationSettings.Find(u => u.UserId == userId).FirstOrDefaultAsync();

                if (notificationConfig == null)
                {
                    _logger.LogError($"NotificationConfig is not found for the user : {userId}");
                    return new ServiceResult(false, "NotificationConfig is not found for the user");
                }

                return new ServiceResult(true, "Notification config fetched Successfully", notificationConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch notification settings for user ID: {UserId}", userId);
                return new ServiceResult(false, "An internal error occurred while fetching notification settings.");
            }

        }

        public async Task<ServiceResult> CreateBulkUsersAsync(IFormFile file)
        {
            try
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension == ".xlsx" || extension == ".xls")
                {
                    var data = await ReadExcelFile(file);
                    return data;
                }
                else if (extension == ".csv")
                {
                    var data = await ReadCsvFile(file);
                    return data;
                }

                return new ServiceResult(false, "Unsupported file type. Please upload .xlsx or .csv.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create bulk users from file.");
                return new ServiceResult(false, "An internal error occurred while creating bulk users.");
            }
        }

        public async Task<ServiceResult> ReadExcelFile(IFormFile file)
        {
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                if (worksheet == null)
                {
                    _logger.LogWarning("No worksheet found in the Excel file.");
                    return new ServiceResult(false, "No worksheet found in the Excel file.");
                }

                var rows = new List<User>();

                foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header row
                {
                    var email = row.Cell(3).GetString();
                    var firstName = row.Cell(1).GetString();
                    var lastName = row.Cell(2).GetString();
                    var roleName = row.Cell(4).GetString();
                    var role = await _roles.Find(r => r.RoleName.ToLower() == roleName.ToLower()).FirstOrDefaultAsync();
                    if (role == null)
                    {
                        _logger.LogWarning($"Role '{roleName}' not found. Skipping user '{email}'.");
                        continue; // Skip users with invalid roles
                    }
                    var user = new User
                    {
                        Email = email,
                        Username = email,
                        FirstName = firstName,
                        LastName = lastName,
                        FullName = $"{firstName} {lastName}",
                        RoleId = role.Id,
                        Role = role.RoleName,
                        PasswordHash = _passwordHelper.HashPassword("trustflow"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    rows.Add(user);
                }

                if (rows.Count == 0)
                {
                    _logger.LogWarning("No valid users found in the Excel file.");
                    return new ServiceResult(false, "No valid users found in the Excel file.");
                }

                await _users.InsertManyAsync(rows);

                return new ServiceResult(true, "Excel file processed successfully.", rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading Excel file for bulk user creation.");
                return new ServiceResult(false, "An error occurred while processing the Excel file.");
            }
        }

        public async Task<ServiceResult> ReadCsvFile(IFormFile file)
        {
            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                var csv = await stream.ReadToEndAsync();
                var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var rows = new List<User>();
                foreach (var line in lines)
                {
                    var columns = line.Split(',');
                    if (columns.Length < 4)
                    {
                        _logger.LogWarning("Invalid CSV format. Each row must have at least 4 columns.");
                        continue;
                    }
                    var email = columns[2];
                    var firstName = columns[0];
                    var lastName = columns[1];
                    var roleName = columns[3];
                    var role = await _roles.Find(r => r.RoleName.ToLower() == roleName.ToLower()).FirstOrDefaultAsync();
                    if (role == null)
                    {
                        _logger.LogWarning($"Role '{roleName}' not found. Skipping user '{email}'.");
                        continue;
                    }

                    var username = email.Split("@")[0].Substring(0, 7);

                    var user = new User
                    {
                        Email = email,
                        Username = username,
                        FirstName = firstName,
                        LastName = lastName,
                        FullName = $"{firstName} {lastName}",
                        RoleId = role.Id,
                        Role = role.RoleName,
                        PasswordHash = _passwordHelper.HashPassword("trustflow"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    rows.Add(user);

                }

                if (rows.Count == 0)
                {
                    _logger.LogWarning("No valid users found in the CSV file.");
                    return new ServiceResult(false, "No valid users found in the CSV file.");
                }

                await _users.InsertManyAsync(rows);
                return new ServiceResult(true, "CSV file processed successfully.", rows);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file for bulk user creation.");
                return new ServiceResult(false, "An error occurred while processing the CSV file.");
            }
        }

        public async Task<ServiceResult> VerifyResetToken(string token)
        {
            try
            {
                var redisData = await _redisCacheService.GetCacheAsync(token);
                if (!redisData.Success)
                {
                    return new ServiceResult(false, "Password Reset Token is not valid", new { state = "invalid" });
                }
                var tokenEncryptedValue = redisData.Result.ToString();
                var tokenBytes = Convert.FromBase64String(tokenEncryptedValue);
                string tokenValue = Encoding.UTF8.GetString(tokenBytes);
                var email = tokenValue.Split("|")[0];
                var expiryTimeString = tokenValue.Split("|")[1];
                var expiryTime = DateTime.Parse(expiryTimeString);

                if (DateTime.UtcNow > expiryTime)
                {
                    return new ServiceResult(false, "Password Reset Token Expired", new { state = "expired" });
                }

                return new ServiceResult(true, "Password Reset Token is Valid", new { state = "valid", email });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate password reset token");
                return new ServiceResult(false, "An internal error occurred while validating the reset token.");
            }
        }


        public async Task<ServiceResult> VerifyResetPassword(PasswordResetDto passwordReset)
        {
            try
            {
                var result = await VerifyResetToken(passwordReset.Token);

                dynamic data = result.Result;

                string email = data.email;

                var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ServiceResult(false, $"User with Email ID '{email}' not found.");
                }

                user.PasswordHash = _passwordHelper.HashPassword(passwordReset.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _users.FindOneAndUpdateAsync(userId => userId.Id == user.Id,
                    Builders<User>.Update
                    .Set(u => u.PasswordHash, user.PasswordHash)
                    .Set(u => u.UpdatedAt, user.UpdatedAt)
                );


                var res = await _redisCacheService.RemoveCacheAsync(passwordReset.Token);

                if (!res.Success)
                {
                    _logger.LogError("Unable to remove token from redis");
                }

                return new ServiceResult(true, "Password Updated Successfully");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password");
                return new ServiceResult(false, "An internal error occurred while resetting the password.");
            }
        }

        public async Task<ServiceResult> GetUserCountAsync()
        {
            try
            {
                var count = await _users.CountDocumentsAsync(_ => true);
                return new ServiceResult(true, "Get User Count Success", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user count.");
                return new ServiceResult(true, ex.Message);
            }
        }
    }
}
