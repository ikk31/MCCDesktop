using MCCDesktop.Instruments;
using MCCDesktop.Models.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MCCDesktop.Views
{
    /// <summary>
    /// Логика взаимодействия для EmployeeInfoWindow.xaml
    /// </summary>
    public partial class EmployeeInfoWindow : Window
    {
        private readonly ApiClient _apiClient;
        private AllEmployees _employee;
        public EmployeeInfoWindow(AllEmployees? employee = null)
        {
            InitializeComponent();
            if (employee == null)
                _employee = new AllEmployees();
            else
                _employee = employee;
                DataContext = _employee;
            _apiClient = new ();
        }
    }
}
