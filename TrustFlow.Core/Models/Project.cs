using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class Project : BaseEntity
    {
        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }

        [BsonElement("LeadUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string LeadUserId { get; set; }

        public string? LeadUserName { get; set; }

        public string? LeadProfilePicUrl { get; set; }

        [BsonElement("ManagerUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ManagerUserId { get; set; }

        public string? ManagerUserName { get; set; }

        public string? ManagerProfilePicUrl { get; set; }

        [BsonElement("Members")]
        public List<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    }
}
