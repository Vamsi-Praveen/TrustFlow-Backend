using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class IssueComment : BaseEntity
    {
        [BsonElement("BugId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string BugId { get; set; }

        [BsonElement("UserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("Text")]
        public string Text { get; set; }
    }
}
