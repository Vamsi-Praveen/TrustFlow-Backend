using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class IssueService
    {
        private readonly IMongoCollection<Issue> _issues;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<IssueStatus> _issueStatus;
        private readonly IMongoCollection<IssuePriority> _issuePriorities;
        private readonly IMongoCollection<IssueType> _issueTypes;
        private readonly IMongoCollection<IssueSeverity> _issueSeverities;
        private readonly IMongoCollection<Counter> _counters;
        private readonly ILogger<IssueService> _logger;

        public IssueService(ApplicationContext context, ILogger<IssueService> logger)
        {
            _issues = context.Issues;
            _users = context.Users;
            _issueStatus = context.IssueStatus;
            _issuePriorities = context.IssuePriorities;
            _issueTypes = context.IssueTypes;
            _issueSeverities = context.IssueSeverities;
            _counters = context.Counters;
            _logger = logger;
        }

        private async Task<int> GetNextSequenceValue(string identifier)
        {
            var filter = Builders<Counter>.Filter.Eq(c => c.Identifier, identifier);
            var update = Builders<Counter>.Update
                .Inc(c => c.Seq, 1)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            var options = new FindOneAndUpdateOptions<Counter>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var counter = await _counters.FindOneAndUpdateAsync(filter, update, options);
            return counter.Seq;
        }

        private async Task<List<IssueDto>> EnrichIssues(List<Issue> issues)
        {
            if (!issues.Any()) return new List<IssueDto>();

            var userIds = issues.SelectMany(i => i.AssigneeUserIds.Append(i.ReporterUserId)).Distinct().ToList();
            var statusIds = issues.Select(i => i.Status).Distinct().ToList();
            var priorityIds = issues.Select(i => i.Priority).Distinct().ToList();
            var typeIds = issues.Select(i => i.Type).Distinct().ToList();
            var severityIds = issues.Select(i => i.Severity).Distinct().ToList();

            var users = await _users.Find(Builders<User>.Filter.In(u => u.Id, userIds)).ToListAsync();
            var statuses = await _issueStatus.Find(Builders<IssueStatus>.Filter.In(s => s.Id, statusIds)).ToListAsync();
            var priorities = await _issuePriorities.Find(Builders<IssuePriority>.Filter.In(p => p.Id, priorityIds)).ToListAsync();
            var types = await _issueTypes.Find(Builders<IssueType>.Filter.In(t => t.Id, typeIds)).ToListAsync();
            var severities = await _issueSeverities.Find(Builders<IssueSeverity>.Filter.In(s => s.Id, severityIds)).ToListAsync();

            var userMap = users.ToDictionary(u => u.Id, u => u.Username ?? u.Email ?? "Unknown");
            var statusMap = statuses.ToDictionary(s => s.Id, s => s.Name);
            var priorityMap = priorities.ToDictionary(p => p.Id, p => p.Name);
            var typeMap = types.ToDictionary(t => t.Id, t => t.Name);
            var severityMap = severities.ToDictionary(s => s.Id, s => s.Name);

            return issues.Select(i => new IssueDto
            {
                Id = i.Id,
                IssueId = i.IssueId,
                Title = i.Title,
                Description = i.Description,
                ProjectId = i.ProjectId,
                Status = new LookupDto { Id = i.Status, Name = statusMap.GetValueOrDefault(i.Status, "Unknown") },
                Priority = new LookupDto { Id = i.Priority, Name = priorityMap.GetValueOrDefault(i.Priority, "Unknown") },
                Type = new LookupDto { Id = i.Type, Name = typeMap.GetValueOrDefault(i.Type, "Unknown") },
                Severity = new LookupDto { Id = i.Severity, Name = severityMap.GetValueOrDefault(i.Severity, "Unknown") },
                Reporter = new LookupDto { Id = i.ReporterUserId, Name = userMap.GetValueOrDefault(i.ReporterUserId, "Unknown") },
                Assignees = i.AssigneeUserIds.Select(a => new LookupDto { Id = a, Name = userMap.GetValueOrDefault(a, "Unknown") }).ToList(),
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList();
        }

        public async Task<ServiceResult> RaiseIssue(Issue newIssue)
        {
            try
            {
                var typeObj = await _issueTypes.Find(Builders<IssueType>.Filter.Eq("_id", new ObjectId(newIssue.Type))).FirstOrDefaultAsync();
                if (typeObj == null) return new ServiceResult(false, "Invalid Issue Type.", null);

                var nextSeq = await GetNextSequenceValue(typeObj.Name.ToUpper());
                newIssue.IssueId = $"{typeObj.Name.ToUpper()}-{nextSeq}";
                newIssue.CreatedAt = DateTime.UtcNow;
                newIssue.UpdatedAt = DateTime.UtcNow;

                await _issues.InsertOneAsync(newIssue);

                var enriched = (await EnrichIssues(new List<Issue> { newIssue })).FirstOrDefault();
                _logger.LogInformation("Issue {IssueId} created successfully.", newIssue.IssueId);

                return new ServiceResult(true, "Issue raised successfully.", enriched);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising new issue.");
                return new ServiceResult(false, "Error raising issue.", null);
            }
        }

        public async Task<ServiceResult> EditIssue(Issue updatedIssue)
        {
            var filter = Builders<Issue>.Filter.Eq(i => i.Id, updatedIssue.Id);
            var result = await _issues.ReplaceOneAsync(filter, updatedIssue);
            if (result.MatchedCount == 0) return new ServiceResult(false, "Issue not found.", null);

            var enriched = (await EnrichIssues(new List<Issue> { updatedIssue })).FirstOrDefault();
            return new ServiceResult(true, "Issue updated successfully.", enriched);
        }

        public async Task<ServiceResult> UpdateIssueStatus(string issueId, string newStatus)
        {
            var result = await _issues.UpdateOneAsync(
                Builders<Issue>.Filter.Eq(i => i.Id, issueId),
                Builders<Issue>.Update.Set(i => i.Status, newStatus).Set(i => i.UpdatedAt, DateTime.UtcNow)
            );

            return result.MatchedCount == 0
                ? new ServiceResult(false, "Issue not found.", null)
                : new ServiceResult(true, "Issue status updated successfully.", null);
        }

        public async Task<ServiceResult> DeleteIssue(string issueId)
        {
            var result = await _issues.DeleteOneAsync(Builders<Issue>.Filter.Eq(i => i.Id, issueId));
            return result.DeletedCount == 0
                ? new ServiceResult(false, "Issue not found.", null)
                : new ServiceResult(true, "Issue deleted successfully.", null);
        }

        public async Task<ServiceResult> GetIssueDetailsAsync(string issueId)
        {
            var issue = await _issues.Find(Builders<Issue>.Filter.Eq(i => i.Id, issueId)).FirstOrDefaultAsync();
            if (issue == null) return new ServiceResult(false, "Issue not found.", null);

            var enriched = (await EnrichIssues(new List<Issue> { issue })).FirstOrDefault();
            return new ServiceResult(true, "Issue details retrieved successfully.", enriched);
        }

        public async Task<ServiceResult> GetIssuesByProjectAsync(string projectId)
        {
            var aggregation = await _issues.Aggregate()
                .Match(i => i.ProjectId == projectId)
                .ToListAsync();

            var enriched = await EnrichIssues(aggregation);
            return new ServiceResult(true, "Project issues retrieved successfully.", enriched);
        }


        public async Task<ServiceResult> GetIssuesReportedByUserAsync(string userId)
        {
            var aggregation = await _issues.Find(Builders<Issue>.Filter.Eq(i => i.ReporterUserId, userId)).ToListAsync();
            var enriched = await EnrichIssues(aggregation);
            return new ServiceResult(true, "Issues reported by user retrieved successfully.", enriched);
        }

        public async Task<ServiceResult> GetIssuesAssignedToUserAsync(string userId)
        {
            var aggregation = await _issues.Find(Builders<Issue>.Filter.AnyEq(i => i.AssigneeUserIds, userId)).ToListAsync();
            var enriched = await EnrichIssues(aggregation);
            return new ServiceResult(true, "Issues assigned to user retrieved successfully.", enriched);
        }

        public async Task<ServiceResult> ProjectWiseIssueAnalytics()
        {
            try
            {
                var allStatuses = await _issueStatus.Find(Builders<IssueStatus>.Filter.Empty).ToListAsync();
                var openStatusIds = allStatuses.Where(s => s.Name != "Closed" && s.Name != "Resolved").Select(s => s.Id).ToList();
                var closedStatusIds = allStatuses.Where(s => s.Name == "Closed" || s.Name == "Resolved").Select(s => s.Id).ToList();

                var aggregation = await _issues.Aggregate()
                    .Group(i => i.ProjectId, g => new
                    {
                        ProjectId = g.Key,
                        Total = g.Count(),
                        Open = g.Count(i => openStatusIds.Contains(i.Status)),
                        Closed = g.Count(i => closedStatusIds.Contains(i.Status))
                    })
                    .ToListAsync();

                var result = aggregation.Select(a => new ProjectIssueAnalyticsDto
                {
                    ProjectId = a.ProjectId,
                    TotalIssues = a.Total,
                    OpenIssues = a.Open,
                    ClosedIssues = a.Closed
                }).ToList();

                return new ServiceResult(true, "Project-wise analytics retrieved successfully.", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project-wise analytics.");
                return new ServiceResult(false, "Error retrieving project-wise analytics.", null);
            }
        }

        public async Task<ServiceResult> UserWiseIssueAnalytics()
        {
            try
            {
                var allStatuses = await _issueStatus.Find(Builders<IssueStatus>.Filter.Empty).ToListAsync();
                var openStatusIds = allStatuses.Where(s => s.Name != "Closed" && s.Name != "Resolved").Select(s => s.Id).ToList();
                var closedStatusIds = allStatuses.Where(s => s.Name == "Closed" || s.Name == "Resolved").Select(s => s.Id).ToList();

                var aggregation = await _issues.Aggregate()
                    .Unwind<Issue, BsonDocument>(i => i.AssigneeUserIds)
                    .Group(new BsonDocument
                    {
                        { "_id", "$AssigneeUserIds" },
                        { "Total", new BsonDocument("$sum", 1) },
                        { "Open", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                            { new BsonDocument("$in", new BsonArray { "$Status", new BsonArray(openStatusIds) }), 1, 0 })) },
                        { "Closed", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                            { new BsonDocument("$in", new BsonArray { "$Status", new BsonArray(closedStatusIds) }), 1, 0 })) }
                    })
                    .ToListAsync();

                var userIds = aggregation.Select(a => a["_id"].AsString).ToList();
                var users = await _users.Find(Builders<User>.Filter.In(u => u.Id, userIds)).ToListAsync();
                var userMap = users.ToDictionary(u => u.Id, u => u.Username ?? u.Email ?? "Unknown");

                var result = aggregation.Select(a => new
                {
                    UserId = a["_id"].AsString,
                    UserName = userMap.GetValueOrDefault(a["_id"].AsString, "Unknown"),
                    TotalIssues = a["Total"].AsInt32,
                    OpenIssues = a["Open"].AsInt32,
                    ClosedIssues = a["Closed"].AsInt32
                }).ToList();

                return new ServiceResult(true, "User-wise analytics retrieved successfully.", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user-wise issue analytics.");
                return new ServiceResult(false, "Error retrieving user-wise issue analytics.", null);
            }
        }
    }
}
