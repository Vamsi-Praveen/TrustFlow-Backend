using Microsoft.AspNetCore.Http;

namespace TrustFlow.Core.DTOs
{
    public class UpdateProfileDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePicUrl { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}
