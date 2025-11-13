using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.DTOs
{
    public class CreateBulkUsers
    {
        [Required(ErrorMessage = "Data file is required.")]
        public IFormFile UsersListFile { get; set; }

        //public IList<User> UsersList { get; set; }
    }
}
