using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Response
{
    public class JobTitleEmployee
    {
        public string? Name { get; set; }
        public double? BaseRate { get; set; }
        public int IdJobTitle { get; set; }
    }
}
