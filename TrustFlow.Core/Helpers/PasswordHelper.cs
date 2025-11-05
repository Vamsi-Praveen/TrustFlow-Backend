namespace TrustFlow.Core.Helpers
{
    public class PasswordHelper
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password,12);
        }

        public bool VerifyPassword(string password, string hash) { 
            return BCrypt.Net.BCrypt.Verify(password,hash);
        }
    }
}
