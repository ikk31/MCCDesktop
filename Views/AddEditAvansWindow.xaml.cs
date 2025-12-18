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
    /// Логика взаимодействия для AddEditAvansWindow.xaml
    /// </summary>
    public partial class AddEditAvansWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly AllAvans _avans;
        private readonly Action _refreshCallback;


        public AddEditAvansWindow(ApiClient apiClient, AllAvans avans, Action refreshCallback = null)
        {

            InitializeComponent();
            _apiClient = apiClient;
            _avans = avans;
            _refreshCallback = refreshCallback;

            Loaded += AddEditAvansWindow_Loaded;

        }
        private async void AddEditAvansWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

    }
}
