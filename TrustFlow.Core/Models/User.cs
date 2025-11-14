using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class User : BaseEntity
    {
        [BsonElement("Username")]
        public string? Username { get; set; }

        [BsonElement("Email")]
        public string Email { get; set; }

        [BsonElement("Password")]
        public string? PasswordHash { get; set; }

        [BsonElement("FullName")]
        public string? FullName { get; set; }

        [BsonElement("PhoneNumber")]
        public string? PhoneNumber { get; set; }

        [BsonElement("ProfilePictureUrl")]
        public string? ProfilePictureUrl { get; set; }

        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("Role")]
        public string Role { get; set; }

        [BsonElement("RoleId")]
        public string RoleId { get; set; }

        [BsonElement("IsActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("DefaultPasswordChanged")]
        public bool DefaultPasswordChanged { get; set; } = false;
    }
}
