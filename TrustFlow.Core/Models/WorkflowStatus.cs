using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class WorkflowStatus : BaseEntity
    {
        [BsonElement("StatusName")]
        public string StatusName { get; set; }

        [BsonElement("Order")]
        public int Order { get; set; } 

        [BsonElement("IsDefault")]
        public bool IsDefault { get; set; } = false; 

        [BsonElement("IsTerminal")]
        public bool IsTerminal { get; set; } = false;

        [BsonElement("ProjectIds")]
        public List<string> ProjectIds { get; set; } = new List<string>();

    }
}
