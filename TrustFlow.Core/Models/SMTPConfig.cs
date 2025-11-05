using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class SMTPConfig : BaseEntity
    {
        [BsonElement("Host")]
        public string Host { get; set; }

        [BsonElement("Port")]
        public int Port { get; set; }

        [BsonElement("EnableSsl")]
        public bool EnableSsl { get; set; }

        [BsonElement("UserName")]
        public string UserName { get; set; }

        [BsonElement("Password")]
        public string Password { get; set; }

        [BsonElement("FromEmail")]
        public string FromEmail { get; set; }

        [BsonElement("DisplayName")]
        public string DisplayName { get; set; }

        [BsonElement("SenderName")]
        public string? SenderName { get; set; }

        [BsonElement("IsActive")]
        public bool IsActive { get; set; }

    }
}
