using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class UserNotificationSetting : BaseEntity
    {
        [BsonElement("UserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("DefaultNotificationMethod")]
        public string DefaultNotificationMethod { get; set; }

        [BsonElement("NotifyOnAssignedBug")]
        public bool NotifyOnAssignedBug { get; set; } = true;

        [BsonElement("NotifyOnStatusChange")]
        public bool NotifyOnStatusChange { get; set; } = true;

        [BsonElement("NotifyOnNewComment")]
        public bool NotifyOnNewComment { get; set; } = true;

    }
}
