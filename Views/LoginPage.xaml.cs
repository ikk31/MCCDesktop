using MCCDesktop.Instruments;
using MCCDesktop.Models.DTOs.Request;
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
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Window
    {
        private readonly ApiClient _apiClient;
        public LoginPage()
        {
            InitializeComponent();
            _apiClient = new();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button)!.IsEnabled = false;
            if (UsernameBox.Text == null || PasswordBox.Password == null)
                MessageBox.Show("Заполните обязательный поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            if(await _apiClient.PostLogin(new UserPasswordDto() { Name = UsernameBox.Text, Password = PasswordBox.Password}))
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            (sender as Button)!.IsEnabled = true;
        }
    }
}
