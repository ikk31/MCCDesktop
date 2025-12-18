using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Request
{
    public class AddPayoutWithLinks
    {
        public int IdEmployee { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string? PeriodName { get; set; }
        public string? Notes { get; set; }
        public List<int> ShiftIds { get; set; } = new();
        public List<int> AvansIds { get; set; } = new(); // Изменено с AdvanceIds на AvansIds
        public decimal TotalEarnings { get; set; }
        public decimal TotalAvans { get; set; } // Изменено с TotalAdvances на TotalAvans
        public decimal NetAmount { get; set; }
        public DateOnly? PaymentDate { get; set; }
    }
}
