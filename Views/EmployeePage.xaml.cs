
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MCCDesktop.Views
{
    using MCCDesktop.HelpClass;
    using MCCDesktop.Instruments;
    using MCCDesktop.Models.DTOs.Request;
    using MCCDesktop.Models.DTOs.Response;
    using Microsoft.Win32;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;


    /// <summary>
    /// Логика взаимодействия для Employee.xaml
    /// </summary>
    public partial class EmployeePage : Page
    {
        private readonly ApiClient _apiClient;
        private List<AllEmployees> _employees;
        public EmployeePage()
        {
            InitializeComponent();
            _apiClient = new ();
            _employees = [];
            GetAllEmployees();
            
        }

        public async void GetAllEmployees()
        {
            DataStorage.CachePhoto.Clear();
            _employees = await _apiClient.GetAllEmployees();
            EmployeesItemsControl.ItemsSource = _employees.Where(x => x.IsDelete == false);
            
        }

      

        private void InfoEmployee_Click(object sender, RoutedEventArgs e)
        {
            var image = (sender as Image);
            var employeeInfoWindow = new EmployeeInfoWindow((sender as Button)!.DataContext as AllEmployees); 

            employeeInfoWindow.ShowDialog();
            GetAllEmployees();
        }

        private void AddEmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            EmployeeInfoWindow employeeInfoWindow = new EmployeeInfoWindow();
            employeeInfoWindow.ShowDialog();
            GetAllEmployees();
        }
   
    }
}

