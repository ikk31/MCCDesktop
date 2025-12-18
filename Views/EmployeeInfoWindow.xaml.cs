using MCCDesktop.HelpClass;
using MCCDesktop.Instruments;
using MCCDesktop.Models.DTOs.Request;
using MCCDesktop.Models.DTOs.Response;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using static System.Net.Mime.MediaTypeNames;

namespace MCCDesktop.Views
{
    /// <summary>
    /// Логика взаимодействия для EmployeeInfoWindow.xaml
    /// </summary>
    public partial class EmployeeInfoWindow : Window
    {
        private readonly ApiClient _apiClient;
        private OpenFileDialog _fileDialog;
        private ThisEmployee _employee;
        public EmployeeInfoWindow(AllEmployees? employee = null)
        {
            InitializeComponent();
            _apiClient = new();
            _employee = new();
            if (employee != null)
            {

                if (!string.IsNullOrEmpty(employee.PhotoPath))
                {
                    try
                    {
                        EmployeePhoto.Source = DataStorage.CachePhoto[employee.PhotoPath];
                    }
                    catch 
                    { 
                    }
                   
                }

                
                
                _employee.IdEmployee = employee.IdEmployee;

            }
                
            else
            {
                _employee.HireDate = DateOnly.FromDateTime(DateTime.Now);
                DataContext = _employee;
            }
               
           
            _fileDialog = new()
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                Multiselect = false
            };
            InitData();
        }
        public async void InitData()
        {
            
            await GetJobTitle();
            if(_employee.IdEmployee != 0)
            await GetThisEmployee();
        }
        
        public async Task GetThisEmployee()
        {
            var employee = await _apiClient.GetThisEmployee(_employee.IdEmployee);
            if (employee != null)
            {
                _employee = employee;
                DataContext = _employee;
            }
            else
            {
                MessageBox.Show("Сотрудник не найден");
                Close();
            }
        }
        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (_fileDialog.ShowDialog() != true || string.IsNullOrEmpty(_fileDialog.FileName)) return;
            initImage(File.ReadAllBytes(_fileDialog.FileName));
        }

        private void initImage(byte[] bytes)
        {
            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            EmployeePhoto.Source = bitmap;
        }


        private async Task GetJobTitle()
        {
            var obj = await _apiClient.GetJobTitle();
            PositionTextBox.ItemsSource = obj;
        }
  


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (PositionTextBox.SelectedItem is not JobTitleEmployee selectedJobTitle)
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }
            if (!HireDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }
            var addEmployees = new ThisEmployee
            {
                Name = _employee.Name,
                LastName = _employee.LastName,
                HireDate = DateOnly.FromDateTime(HireDatePicker.SelectedDate.Value),
                IdJobTitle = selectedJobTitle.IdJobTitle,
                IsDelete = false

            };
            if (string.IsNullOrEmpty(FirstNameTextBox.Text) ||
          string.IsNullOrEmpty(LastNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля: Имя и Фамилия",
                              "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaveButton.IsEnabled = false;
            if (_employee.IdEmployee == 0)
            {
                await _apiClient.PostEmployee(addEmployees, _fileDialog);
                Close();
            }
            else
            {
                await _apiClient.PutEmployee(_employee.IdEmployee, _employee, _fileDialog);
                Close();
            }
                SaveButton.IsEnabled = true;
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var obj = _employee;
            await _apiClient.DeleteEmployee(_employee.IdEmployee);
            Close();
        }
    }
}
