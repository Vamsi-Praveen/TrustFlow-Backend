using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class User : BaseEntity
    {
        [BsonElement("Username")]
        public string Username { get; set; }

        [BsonElement("Email")]
        public string Email { get; set; }

        [BsonElement("Password")]
        public string PasswordHash { get; set; }

        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("Roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [BsonElement("IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
