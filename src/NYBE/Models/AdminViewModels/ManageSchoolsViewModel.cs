﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NYBE.Models.AdminViewModels
{
    public class ManageSchoolsViewModel
    {
        public List<PendingSchool> pendingSchools { get; set; }
        public List<School> allSchools { get; set; }
        public List<School> disabledSchools { get; set; }
    }
}
