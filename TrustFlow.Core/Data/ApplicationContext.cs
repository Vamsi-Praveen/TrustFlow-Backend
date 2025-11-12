using MongoDB.Driver;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Data
{
    public class ApplicationContext
    {
        private readonly IMongoDatabase _database;

        public ApplicationContext(MongoDbSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        public IMongoCollection<Project> Projects => _database.GetCollection<Project>("Projects");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Issue> Issues => _database.GetCollection<Issue>("Issues");
        public IMongoCollection<IssueComment> Comments => _database.GetCollection<IssueComment>("Comments");
        public IMongoCollection<IssueAttachment> Attachments => _database.GetCollection<IssueAttachment>("Attachments");
        public IMongoCollection<IssuePriority> IssuePriorities => _database.GetCollection<IssuePriority>("IssuePriorities");
        public IMongoCollection<IssueSeverity> IssueSeverities => _database.GetCollection<IssueSeverity>("IssueSeverities");
        public IMongoCollection<IssueType> IssueTypes => _database.GetCollection<IssueType>("IssueTypes");
        public IMongoCollection<WorkflowStatus> WorkflowStatuses => _database.GetCollection<WorkflowStatus>("WorkflowStatuses");
        public IMongoCollection<RolePermission> RolePermissions => _database.GetCollection<RolePermission>("RolePermissions");
        public IMongoCollection<UserNotificationSetting> UserNotificationSettings => _database.GetCollection<UserNotificationSetting>("UserNotificationSettings");
        public IMongoCollection<SlackConfig> SlackConfig => _database.GetCollection<SlackConfig>("SlackConfig");
        public IMongoCollection<TeamsConfig> TeamsConfig => _database.GetCollection<TeamsConfig>("TeamsConfig");
        public IMongoCollection<SMTPConfig> SMTPConfig => _database.GetCollection<SMTPConfig>("SMTPConfig");
        public IMongoCollection<IssueStatus> IssueStatus => _database.GetCollection<IssueStatus>("IssueStatus");
        public IMongoCollection<ActivityLog> ActivityLog => _database.GetCollection<ActivityLog>("ActivityLog");
        public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("Counters");

    }
}
