using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class IssueService
    {
        private readonly IMongoCollection<Issue> _issues;
        private readonly IMongoCollection<User> _users;
        private readonly ILogger<IssueService> _logger;

        public IssueService(ApplicationContext context, ILogger<IssueService> logger)
        {
            _issues = context.Issues;
            _users = context.Users;
            _logger = logger;
        }


        public async Task<ServiceResult> GetOpenIssuesCountByUserAsync(string userId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.And(
                Builders<Issue>.Filter.AnyEq(i => i.AssigneeUserIds, userId),
                Builders<Issue>.Filter.Ne(i => i.Status, "Closed"));

                var count = await _issues.CountDocumentsAsync(filter);
                _logger.LogInformation("User {UserId} has {Count} open issues assigned.", userId, count);
                return new ServiceResult(true, "Get Open Issues Count by User Id Successful", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving open issues count for user {UserId}.", userId);
                return new ServiceResult(false, "Error retrieving open issues count.", null);
            }
        }


        public async Task<ServiceResult> GetIssuesByProjectAsync(string projectId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.ProjectId, projectId);
                var issues = await _issues.Find(filter).ToListAsync();
                _logger.LogInformation("Retrieved {Count} issues for project {ProjectId}.", issues.Count, projectId);
                return new ServiceResult(true, "Get Issues by Project Id Successful", issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issues for project {ProjectId}.", projectId);
                return new ServiceResult(false, "Error retrieving issues for project.", null);
            }
        }

        public async Task<ServiceResult> IssueAnalytics()
        {
            try
            {
                var totalIssues = await _issues.CountDocumentsAsync(FilterDefinition<Issue>.Empty);
                var openIssues = await _issues.CountDocumentsAsync(Builders<Issue>.Filter.Ne(i => i.Status, "Closed"));
                var closedIssues = await _issues.CountDocumentsAsync(Builders<Issue>.Filter.Eq(i => i.Status, "Closed"));
                var analytics = new
                {
                    TotalIssues = totalIssues,
                    OpenIssues = openIssues,
                    ClosedIssues = closedIssues
                };
                _logger.LogInformation("Issue analytics retrieved successfully.");
                return new ServiceResult(true, "Issue Analytics Successful", analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issue analytics.");
                return new ServiceResult(false, "Error retrieving issue analytics.", null);
            }
        }

        public async Task<ServiceResult> ProjectIssueAnalytics(string projectId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.ProjectId, projectId);
                var totalIssues = await _issues.CountDocumentsAsync(filter);
                var openIssues = await _issues.CountDocumentsAsync(Builders<Issue>.Filter.And(
                    filter,
                    Builders<Issue>.Filter.Ne(i => i.Status, "Closed")));
                var closedIssues = await _issues.CountDocumentsAsync(Builders<Issue>.Filter.And(
                    filter,
                    Builders<Issue>.Filter.Eq(i => i.Status, "Closed")));
                var analytics = new
                {
                    TotalIssues = totalIssues,
                    OpenIssues = openIssues,
                    ClosedIssues = closedIssues
                };
                _logger.LogInformation("Project {ProjectId} issue analytics retrieved successfully.", projectId);
                return new ServiceResult(true, "Project Issue Analytics Successful", analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issue analytics for project {ProjectId}.", projectId);
                return new ServiceResult(false, "Error retrieving project issue analytics.", null);
            }
        }


        public async Task<ServiceResult> ProjectWiseIssueAnalytics()
        {
            try
            {
                var projectGroups = await _issues.Aggregate()
                    .Group(i => i.ProjectId, g => new
                    {
                        ProjectId = g.Key,
                        TotalIssues = g.Count(),
                        OpenIssues = g.Count(i => i.Status != "Closed"),
                        ClosedIssues = g.Count(i => i.Status == "Closed")
                    })
                    .ToListAsync();
                _logger.LogInformation("Project-wise issue analytics retrieved successfully.");
                return new ServiceResult(true, "Project Wise Issue Analytics Successful", projectGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project-wise issue analytics.");
                return new ServiceResult(false, "Error retrieving project-wise issue analytics.", null);
            }
        }


        public async Task<ServiceResult> GetIssueDetails(string issueId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.Id, issueId);
                var issue = await _issues.Find(filter).FirstOrDefaultAsync();
                if (issue == null)
                {
                    _logger.LogWarning("Issue {IssueId} not found.", issueId);
                    return new ServiceResult(false, "Issue not found.", null);
                }
                _logger.LogInformation("Retrieved details for issue {IssueId}.", issueId);
                return new ServiceResult(true, "Get Issue Details Successful", issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving details for issue {IssueId}.", issueId);
                return new ServiceResult(false, "Error retrieving issue details.", null);
            }
        }

        public async Task<ServiceResult> GetProjectIssuesByStatus(string projectId, string status)
        {
            try
            {
                var filter = Builders<Issue>.Filter.And(
                    Builders<Issue>.Filter.Eq(i => i.ProjectId, projectId),
                    Builders<Issue>.Filter.Eq(i => i.Status, status));
                var issues = await _issues.Find(filter).ToListAsync();
                _logger.LogInformation("Retrieved {Count} issues for project {ProjectId} with status {Status}.", issues.Count, projectId, status);
                return new ServiceResult(true, "Get Project Issues by Status Successful", issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issues for project {ProjectId} with status {Status}.", projectId, status);
                return new ServiceResult(false, "Error retrieving project issues by status.", null);
            }
        }


        public async Task<ServiceResult> GetIssuesReportedByUserAsync(string userId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.ReporterUserId, userId);
                var issues = await _issues.Find(filter).ToListAsync();
                _logger.LogInformation("Retrieved {Count} issues reported by user {UserId}.", issues.Count, userId);
                return new ServiceResult(true, "Get Issues Reported by User Id Successful", issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issues reported by user {UserId}.", userId);
                return new ServiceResult(false, "Error retrieving issues reported by user.", null);
            }
        }

        public async Task<ServiceResult> GetIssuesAssignedToUserAsync(string userId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.AnyEq(i => i.AssigneeUserIds, userId);
                var issues = await _issues.Find(filter).ToListAsync();
                _logger.LogInformation("Retrieved {Count} issues assigned to user {UserId}.", issues.Count, userId);
                return new ServiceResult(true, "Get Issues Assigned to User Id Successful", issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issues assigned to user {UserId}.", userId);
                return new ServiceResult(false, "Error retrieving issues assigned to user.", null);
            }
        }


        public async Task<ServiceResult> GetIssueDetailsAsync(string issueId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.Id, issueId);
                var issue = await _issues.Find(filter).FirstOrDefaultAsync();
                if (issue == null)
                {
                    _logger.LogWarning("Issue {IssueId} not found.", issueId);
                    return new ServiceResult(false, "Issue not found.", null);
                }
                _logger.LogInformation("Retrieved details for issue {IssueId}.", issueId);
                return new ServiceResult(true, "Get Issue Details Successful", issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving details for issue {IssueId}.", issueId);
                return new ServiceResult(false, "Error retrieving issue details.", null);
            }
        }


        public async Task<ServiceResult> RaiseIssue(Issue newIssue)
        {
            try
            {
                await _issues.InsertOneAsync(newIssue);
                _logger.LogInformation("New issue {IssueId} raised successfully.", newIssue.Id);
                return new ServiceResult(true, "Issue raised successfully.", newIssue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising new issue.");
                return new ServiceResult(false, "Error raising new issue.", null);
            }
        }

        public async Task<ServiceResult> UpdateIssueStatus(string issueId, string newStatus)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.Id, issueId);
                var update = Builders<Issue>.Update.Set(i => i.Status, newStatus);
                var result = await _issues.UpdateOneAsync(filter, update);
                if (result.MatchedCount == 0)
                {
                    _logger.LogWarning("Issue {IssueId} not found for status update.", issueId);
                    return new ServiceResult(false, "Issue not found.", null);
                }
                _logger.LogInformation("Issue {IssueId} status updated to {NewStatus}.", issueId, newStatus);
                return new ServiceResult(true, "Issue status updated successfully.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for issue {IssueId}.", issueId);
                return new ServiceResult(false, "Error updating issue status.", null);
            }
        }

        public async Task<ServiceResult> DeleteIssue(string issueId)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.Id, issueId);
                var result = await _issues.DeleteOneAsync(filter);
                if (result.DeletedCount == 0)
                {
                    _logger.LogWarning("Issue {IssueId} not found for deletion.", issueId);
                    return new ServiceResult(false, "Issue not found.", null);
                }
                _logger.LogInformation("Issue {IssueId} deleted successfully.", issueId);
                return new ServiceResult(true, "Issue deleted successfully.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting issue {IssueId}.", issueId);
                return new ServiceResult(false, "Error deleting issue.", null);
            }
        }


        public async Task<ServiceResult> EditIssue(Issue updatedIssue)
        {
            try
            {
                var filter = Builders<Issue>.Filter.Eq(i => i.Id, updatedIssue.Id);
                var result = await _issues.ReplaceOneAsync(filter, updatedIssue);
                if (result.MatchedCount == 0)
                {
                    _logger.LogWarning("Issue {IssueId} not found for update.", updatedIssue.Id);
                    return new ServiceResult(false, "Issue not found.", null);
                }
                _logger.LogInformation("Issue {IssueId} updated successfully.", updatedIssue.Id);
                return new ServiceResult(true, "Issue updated successfully.", updatedIssue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating issue {IssueId}.", updatedIssue.Id);
                return new ServiceResult(false, "Error updating issue.", null);
            }
        }

    }
}
