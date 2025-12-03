using MCCDesktop.Models.DTOs.Request;
using MCCDesktop.Models.DTOs.Response;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

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
            _httpClient.BaseAddress = new Uri("https://192.168.0.158:7106/");
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



        public async Task PostEmployee(ThisEmployee employees, OpenFileDialog? dialog = null)
        {
            try
            {
                if (dialog != null && !string.IsNullOrEmpty(dialog.FileName))
                {
                    byte[] bytes = File.ReadAllBytes(dialog.FileName);
                    using (var content = new MultipartFormDataContent())
                    {
                        ByteArrayContent imageContent = new ByteArrayContent(bytes);
                        content.Add(imageContent, "file", System.IO.Path.GetFileName(dialog.FileName));
                        var responseFile = await _httpClient.PostAsync("api/AddPhoto/AddPhoto", content);
                        if (responseFile.IsSuccessStatusCode)
                        {
                            employees.PhotoPath = await responseFile.Content.ReadAsStringAsync();
                        }
                    }
                }
                var response = await _httpClient.PostAsJsonAsync("api/Employee/AddEmployee", employees);
                var messageResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    MessageBox.Show(messageResponse, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(messageResponse, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<byte[]> GetPhoto(string photoPath)
        {
            var response = await _httpClient.GetAsync($"api/AddPhoto/Photo/{photoPath}");
            if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
        }

       
       public async Task<ThisEmployee?> GetThisEmployee(int idEmployee)
        {
            var response = await _httpClient.GetAsync($"api/Employee/GetThisEmployee/{idEmployee}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ThisEmployee>();
            }
            return new ThisEmployee();
        }

        public async Task PutEmployee(int IdEmployee, ThisEmployee thisEmployee, FileDialog? dialog)
        {
            HttpResponseMessage responsePhoto;
            if (dialog != null && !string.IsNullOrEmpty(dialog.FileName))
            {
                byte[] bytes = File.ReadAllBytes(dialog.FileName);

                using (var content = new MultipartFormDataContent())
                {
                    ByteArrayContent imageContent = new ByteArrayContent(bytes);
                    content.Add(imageContent, "file", System.IO.Path.GetFileName(dialog.FileName));
                    if (string.IsNullOrEmpty(thisEmployee.PhotoPath))
                    {
                         responsePhoto = await _httpClient.PostAsync("api/AddPhoto/AddPhoto", content);
                    }
                    else
                    {
                        responsePhoto = await _httpClient.PutAsync($"api/AddPhoto/EditPhoto/{thisEmployee.PhotoPath}", content);

                    }
                    if (responsePhoto.IsSuccessStatusCode)
                    {
                        thisEmployee.PhotoPath = await responsePhoto.Content.ReadAsStringAsync();
                    }

                }
            }
            
            var response = await _httpClient.PutAsJsonAsync($"api/Employee/PutEmployee/{IdEmployee}", thisEmployee);

            var messageResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show(messageResponse, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(messageResponse, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public async Task<List<JobTitleEmployee>?> GetJobTitle()
        {
            var response = await _httpClient.GetAsync("api/Reference/AllJobTitle");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<JobTitleEmployee>>();
            }
            return null;

        }

      public async Task PostShifts(AddShifts addShifts)
        {
            try
            {
            var response = await _httpClient.PostAsJsonAsync("api/WorkHours/AddShift", addShifts);
            var messageResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                MessageBox.Show(messageResponse, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(messageResponse, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<List<AllWorkPlaces>> GetAllWorkplaces()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Reference/AllWorkPlace");
                if (response.IsSuccessStatusCode)
                {
                    var workplaces = await response.Content.ReadFromJsonAsync<List<AllWorkPlaces>>();
                    return workplaces ?? new List<AllWorkPlaces>();
                }
                return new List<AllWorkPlaces>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении рабочих точек: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<AllWorkPlaces>();
            }
        }

        public async Task<AllShifts> GetShiftById(int idShift)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/WorkHours/GetShift/{idShift}");
                if (response.IsSuccessStatusCode)
                {
                    var shift = await response.Content.ReadFromJsonAsync<AllShifts>();
                    return shift!;
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении смены: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public async Task PutShift(int IdShift, AddShifts addShifts)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/WorkHours/PutShift/{IdShift}", addShifts);
                var messageResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(messageResponse, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                }
                else
                {
                    MessageBox.Show(messageResponse, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
            }
        }

        public class ShiftCalculator
        {
            public static (double WorkHours, decimal TotalEarned) CalculateShift(
                TimeSpan startTime,
                TimeSpan endTime,
                int breakDurationMinutes,
                int hourlyRate)
            {
                // Рассчитываем общее время
                var totalDuration = endTime - startTime;
                if (totalDuration.TotalHours <= 0)
                    return (0, 0);

                // Рассчитываем время перерыва
                var breakTime = TimeSpan.FromMinutes(breakDurationMinutes);

                // Рассчитываем фактически отработанные часы
                var workedTime = totalDuration - breakTime;
                if (workedTime.TotalHours < 0)
                    workedTime = TimeSpan.Zero;

                double workHours = Math.Round(workedTime.TotalHours, 2);
                decimal totalEarned = Math.Round((decimal)workHours * hourlyRate, 2);

                return (workHours, totalEarned);
            }
        }

        public async Task RecalculateAllShifts()
        {
            try
            {
                var allShifts = await GetAllShifts();

                foreach (var shift in allShifts)
                {
                    if (shift.StartTime.HasValue && shift.EndTime.HasValue &&
                        shift.HourlyRate.HasValue && shift.BreakDuration.HasValue)
                    {
                        var (workHours, totalEarned) = ShiftCalculator.CalculateShift(
                            shift.StartTime.Value,
                            shift.EndTime.Value,
                            shift.BreakDuration.Value,
                            shift.HourlyRate.Value
                        );

                        // Создаем DTO для обновления
                        var updateShift = new AddShifts
                        {
                            IdShifts = shift.IdShifts,
                            IdEmployee = shift.IdEmployee ?? 0,
                            IdWorkplace = shift.IdWorkplace ?? 0,
                            Date = shift.Date,
                            StartTime = shift.StartTime,
                            EndTime = shift.EndTime,
                            HourlyRate = shift.HourlyRate,
                            BreakDuration = shift.BreakDuration,
                            Notes = shift.Notes,
                            WorkHours = workHours,
                            TotalEarned = totalEarned
                        };

                        await PutShift(shift.IdShifts, updateShift);
                    }
                }

                MessageBox.Show("Перерасчет завершен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перерасчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
