using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Response
{
    public class AllEmployees
    {
        public int IdEmployee { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? PhotoPath { get; set; }
        public bool? IsDelete { get; set; }
        public string FullName
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(Name)) parts.Add(Name);
                if (!string.IsNullOrEmpty(LastName)) parts.Add(LastName);
                return string.Join(" ", parts);
            }
        }

    }


  


}
