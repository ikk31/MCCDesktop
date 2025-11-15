using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCCDesktop.Models.DTOs.Response
{
    public class AllShifts
    {
        public int idShifts { get; set; }
        public DateOnly? Date { get; set; }
        //public DateOnly? StartTime { get; set; }
        //public TimeOnly? EndTime { get; set; }
        //public int? BreakDuration { get; set; }
        //public int? ActualDuration { get; set; }
        public int? HourlyRate { get; set; }
        //public double? WorkHours { get; set; }

        public int? IdEmployee { get; set; }
        public EmployeeDto IdEmployeeNavigation { get; set; }
    }

    public class EmployeeDto
    {
        public string? Name { get; set; }
    }
}

