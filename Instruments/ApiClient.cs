using MCCDesktop.Models.DTOs.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MCCDesktop.Instruments
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private static HttpClient createClientWithoutCert()
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (message,cert,chain,errors) => true};
            var client = new HttpClient(handler);
            return client;
        }
        public ApiClient() 
        {
            _httpClient = createClientWithoutCert();
            _httpClient.BaseAddress = new Uri("https://192.168.0.167:7106/");
        }
        public async Task<List<AllEmployees>> GetAllEmployees()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Employee/AllEmployee");
                if (response.IsSuccessStatusCode)
                {
                    var employees = await response.Content.ReadFromJsonAsync<List<AllEmployees>>();
                    return employees!;
                }
                var Message = await response.Content.ReadAsStringAsync();
                MessageBox.Show(Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<AllEmployees>();
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<AllEmployees>();
                
            }
        }

        public async Task<List<AllShifts>> GetAllShifts()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/WorkHours/AllShifts");
                if (response.IsSuccessStatusCode)
                {
                    var shifts = await response.Content.ReadFromJsonAsync<List<AllShifts>>();
                    return shifts!;
                }
                var Message = await response.Content.ReadAsStringAsync();
                MessageBox.Show(Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<AllShifts>();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<AllShifts>();
            }
        }

    }
}
