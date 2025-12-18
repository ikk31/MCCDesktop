using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Response
{
    public class AllShifts
    {
        public int IdShifts { get; set; }
        public DateOnly? Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? BreakDuration { get; set; }
        public int? ActualDuration { get; set; }
        public int? HourlyRate { get; set; }

        public int? IdWorkplace { get; set; }
        public double? WorkHours { get; set; }
        public AllWorkPlaces? IdWorkplaceNavigation { get; set; }

        public decimal? TotalEarned { get; set; }

        public int? IdEmployee { get; set; }
        public EmployeeDto? IdEmployeeNavigation { get; set; }

        public string? Notes { get; set; }

        public bool? IsDelete { get; set; }
    }

    public class EmployeeDto
    {
        public string? Name { get; set; }

    }



}

