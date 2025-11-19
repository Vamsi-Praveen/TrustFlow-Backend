using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class ActivityLog : BaseEntity
    {
        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }

        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        [BsonElement("entityType")]
        public string EntityType { get; set; } = string.Empty;

        [BsonElement("entityId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? EntityId { get; set; }

        [BsonElement("action")]
        public string Action { get; set; } = string.Empty;

        [BsonElement("oldValue")]
        public string? OldValue { get; set; }

        [BsonElement("newValue")]
        public string? NewValue { get; set; }

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }

        [BsonElement("userAgent")]
        public string? UserAgent { get; set; }

        [BsonElement("source")]
        public string Source { get; set; } = "Web";

        [BsonElement("status")]
        public string Status { get; set; } = "Success";

        [BsonElement("correlationId")]
        public string? CorrelationId { get; set; }
    }
}

