using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class IssueType
    {
        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }

        [BsonElement("IsDefault")]
        public bool IsDefault { get; set; } = false;
    }
}
