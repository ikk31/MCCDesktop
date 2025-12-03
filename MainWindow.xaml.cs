using MCCDesktop.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace MCCDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isNavPanelVisible = true;
        public MainWindow()
        {
            InitializeComponent();
        }

        //анимация панели слева
        private void TogglePanelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isNavPanelVisible)
            {
                // Скрываем панель
                AnimateNavPanel(-NavPanel.ActualWidth);
                isNavPanelVisible = false;
                TogglePanelBtn.Content = "☰";
            }
            else
            {
                // Показываем панель
                AnimateNavPanel(0);
                isNavPanelVisible = true;
                TogglePanelBtn.Content = "✕";
            }
        }

        private void AnimateNavPanel(double toValue)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            NavPanelTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void EmployeesBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new EmployeePage());
        }

        private void WorkCalendar_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ShiftsCal());
        }

        private void TimeTrackingBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HoursPage());
        }

        private void SalariesBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SalariesPage());
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}