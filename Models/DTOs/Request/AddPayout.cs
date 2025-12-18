using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Request
{
    
        public class AddPayout
        {
            public int IdEmployee { get; set; }
            public DateOnly? PeriodStart { get; set; }
            public DateOnly? PeriodEnd { get; set; }
            public string? PeriodName { get; set; }
            public List<int> ShiftIds { get; set; } = new();
            public List<int> AvansIds { get; set; } = new();
            public int? TotalHours { get; set; }
            public decimal? TotalAvans { get; set; }
            public decimal? TotalAmount { get; set; }
            public string? Notes { get; set; }
            public DateOnly? PaidAt { get; set; }
        }
    }

