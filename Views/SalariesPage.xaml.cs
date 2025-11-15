using System.Windows.Controls;

namespace MCCDesktop.Views
{
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Логика взаимодействия для SalariesPage.xaml
    /// </summary>
    public partial class SalariesPage : Page
    {
        HttpClient httpClient;
        Uri uri;
        public SalariesPage()
        {
            InitializeComponent();
            uri = new Uri(" https://r88i3e-176-116-188-143.ru.tuna.am");
            var handler = new HttpClientHandler();

            httpClient = new HttpClient(handler);

            getDbEmployee();
        }

        async Task getDbEmployee()
        {
            


        }
    }
}
