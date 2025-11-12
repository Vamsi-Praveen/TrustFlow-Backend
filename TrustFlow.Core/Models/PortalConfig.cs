using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class PortalConfig : BaseEntity
    {
        [BsonElement("DefaultNotificationMethod")]
        public string DefaultNotificationMethod { get; set; } = "email";
    }
}
