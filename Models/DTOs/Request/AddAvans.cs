using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Request
{
    public class AddAvans
    {
        public int IdAvans { get; set; }
        public int IdEmployee { get; set; }
        public DateOnly? Date { get; set; }
        public decimal? Amount { get; set; }
        public bool? IsDelete { get; set; }
    }
}
