using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class IssueType : BaseEntity
    {
        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }
    }
}
