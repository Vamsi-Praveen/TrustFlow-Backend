using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustFlow.Core.DTOs
{
    public class PasswordResetDto
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
