using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class IssueSeverity : BaseEntity
    {
        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }

        [BsonElement("Order")]
        public int Order { get; set; }

        [BsonElement("IsDefault")]
        public bool IsDefault { get; set; } = false;

        [BsonElement("ProjectIds")]
        public List<string> ProjectIds { get; set; } = new List<string>();
    }
}
