using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class ProjectMember : BaseEntity
    {
        [BsonElement("UserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("RoleId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RoleId { get; set; }

        [BsonElement("Role")]
        public string RoleName { get; set; }

        public string? UserName{ get; set; }

        public string? UserEmail { get; set; }

        public string? ProfilePicUrl { get; set; }
    }
}
