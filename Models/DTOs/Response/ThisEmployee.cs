using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Response
{
    public class ThisEmployee
    {
     
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public DateOnly? HireDate { get; set; }
        public int? IdJobTitle { get; set; }
        public bool? IsDelete { get; set; }
        public int IdEmployee { get; set; }
        public string? PhotoPath { get; set; }

        public JobTitleEmployee? IdJobTitleNavigation { get; set; }
    }

   
}
