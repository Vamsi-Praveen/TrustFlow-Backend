using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class IssueAttachment : BaseEntity
    {
        [BsonElement("BugId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string BugId { get; set; }

        [BsonElement("FileName")]
        public string FileName { get; set; }

        [BsonElement("FilePath")]
        public string FilePath { get; set; }

        [BsonElement("UploadedByUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UploadedByUserId { get; set; }
    }
}
