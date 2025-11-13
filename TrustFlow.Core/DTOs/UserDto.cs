using Microsoft.AspNetCore.Http;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustFlow.Core.DTOs
{
    public class UserDto
    {
        public string Username { get; set; }

        public string Email { get; set; }
        public string? PasswordHash { get; set; }

        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public IFormFile? ProfileImage { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Role { get; set; }

        public string RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool DefaultPasswordChanged { get; set; } = false;
    }
}
