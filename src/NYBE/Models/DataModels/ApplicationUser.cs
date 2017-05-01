﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace NYBE.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public double Rating { get; set; }
        public int SchoolID { get; set; }
        public int PreferredContact { get; set; } // 0 for email, 1 for phone call, 2 for phone text
        public int Status { get; set; } // 0 for inactive, 1 for active

        public School School { get; set; }

    }
}
