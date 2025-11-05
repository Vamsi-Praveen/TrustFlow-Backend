using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class TeamsConfig : BaseEntity
    {
        [BsonElement("TenantId")]
        public string TenantId { get; set; }

        [BsonElement("ClientSecret")]
        public string ClientSecret {get; set; }

        [BsonElement("ClientId")]
        public string ClientId {get; set; }

        [BsonElement("Scope")]
        public string Scope { get; set; }

        [BsonElement("GrantType")]
        public string GrantType { get; set; }

        [BsonElement("TokenUrl")]
        public string TokenUrl { get; set; }

        [BsonElement("IsActive")]
        public bool IsActive { get; set; }
    }
}
