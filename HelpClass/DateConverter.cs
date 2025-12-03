using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MCCDesktop.HelpClass
{
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly date && value != null)
            {
                return new DateTime(date.Year, date.Month, date.Day);
            }
            return DateTime.Now;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date && value != null)
            {
                return DateOnly.FromDateTime(date);
            }
            return DateOnly.FromDateTime(DateTime.Now);
        }
    }
}
