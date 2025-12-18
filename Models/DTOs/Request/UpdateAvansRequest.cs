using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Request
{
    public class UpdateAvansRequest
    {
        public DateOnly? Date { get; set; }
        public decimal? Amount { get; set; }  
    }
}
