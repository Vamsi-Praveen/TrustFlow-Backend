using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class ActivityLog
    {
        [BsonElement("Id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; } 

        [BsonElement("entityType")]
        public string EntityType { get; set; } = string.Empty; 

        [BsonElement("entityId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EntityId { get; set; } = string.Empty;

        [BsonElement("actionType")]
        public string ActionType { get; set; } = string.Empty; 

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

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
