namespace TrustFlow.Core.DTOs
{
    public class ChangePassword
    {
        public string? UserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
