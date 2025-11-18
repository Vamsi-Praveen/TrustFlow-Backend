using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class Issue : BaseEntity
    {
        [BsonElement("IssueId")]
        public string? IssueId { get; set; }

        [BsonElement("Title")]
        public string Title { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }

        [BsonElement("ProjectId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }

        [BsonElement("ReporterUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ReporterUserId { get; set; }

        [BsonElement("AssigneeUserIds")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> AssigneeUserIds { get; set; }

        [BsonElement("Status")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Status { get; set; }

        [BsonElement("Priority")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Priority { get; set; }

        [BsonElement("Severity")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Severity { get; set; }

        [BsonElement("Type")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Type { get; set; }

        [BsonElement("Comments")]
        public List<IssueComment> Comments { get; set; } = new List<IssueComment>();

        [BsonElement("Attachments")]
        public List<IssueAttachment> Attachments { get; set; } = new List<IssueAttachment>();

        [BsonElement("LinkedIssues")]
        public List<string> LinkedIssues { get; set; } = new List<string>();
    }
}
