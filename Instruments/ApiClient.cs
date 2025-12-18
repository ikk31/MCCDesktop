using MCCDesktop.HelpClass;
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
using System.Net.Http.Headers;
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
            _httpClient.BaseAddress = new Uri("https://192.168.0.200:7106/");
            if (!string.IsNullOrEmpty(DataStorage.Token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DataStorage.Token);
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

        public async Task<byte[]?> GetPhoto(string photoPath)
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

        public async Task<AllShifts?> GetShiftById(int idShift)
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

        public async Task<List<AllShifts>> GetShiftsByEmployeeAndPeriod(
    int employeeId, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var url = $"api/Reference/shifts/{employeeId}/period" +
                         $"?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var shifts = await response.Content.ReadFromJsonAsync<List<AllShifts>>();
                return shifts ?? new List<AllShifts>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Ошибка сети: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения смен: {ex.Message}");
            }
        }

        public async Task<List<AllAvans>> GetAvansByEmployeeAndPeriod(
        int employeeId, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                // Формируем URL запроса
                var url = $"api/Reference/avans/{employeeId}/period" +
                         $"?startDate={startDate:yyyy-MM-dd}" +
                         $"&endDate={endDate:yyyy-MM-dd}";

                // Выполняем GET запрос
                var response = await _httpClient.GetAsync(url);

                // Проверяем успешность запроса
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка API: {response.StatusCode} - {errorContent}");
                }

                // Десериализуем ответ
                var avansList = await response.Content.ReadFromJsonAsync<List<AllAvans>>();

                // Проверяем на null и возвращаем результат
                return avansList ?? new List<AllAvans>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Ошибка сети при получении авансов: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Ошибка парсинга JSON при получении авансов: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении авансов: {ex.Message}");
            }
        }

        public async Task<bool> DeleteEmployee(int IdEmployee)
        {
            try
            {
                var url = $"api/Employee/DeleteEmployee/{IdEmployee}";
                var response = await _httpClient.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responsecontent = await response.Content.ReadAsStringAsync();
                    return true;
                }
                else
                {
                    var responsecontent = await response.Content.ReadAsStringAsync();
                    throw new Exception(responsecontent);
                }
            }
            catch (Exception ex)  
            {
                throw new Exception($"ERROR!!!!!!! - {ex.Message}");
            }
        }

        public async Task<bool> CreateAvans(AddAvans avansDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Reference/AddAvans", avansDto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAvans(int avansId, UpdateAvansRequest dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/Reference/avans/{avansId}/update", dto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAvans(int IdAvans)
        {
            try
            {
                var url = $"api/Reference/DeleteAvans/{IdAvans}";
                var response = await _httpClient.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responsecontent = await response.Content.ReadAsStringAsync();
                    return true;
                }
                else
                {
                    var responsecontent = await response.Content.ReadAsStringAsync();
                    throw new Exception(responsecontent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR!!!!!!! - {ex.Message}");
            }
        }

        public async Task<bool> DeleteShifts(int IdShift)
        {
            try
            {
                var url = $"api/WorkHours/DeleteShift/{IdShift}";
                var response = await _httpClient.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responsecontent = await response.Content.ReadAsStringAsync();
                    return true;
                }
                else
                {
                    var responsecontent = await response.Content.ReadAsStringAsync();
                    throw new Exception(responsecontent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR!!!!!!! - {ex.Message}");
            }
        }

        public async Task<bool> CreatePayout(AddPayout dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Payout/create", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка API: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                //if (result != null && result.TryGetValue("IdPayout", out var id))
                //{
                //    return Convert.ToInt32(id);
                //}

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания выплаты: {ex.Message}");
            }
        }

        public async Task<bool> PostLogin(UserPasswordDto userPasswordDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/Autorization/Autorization", userPasswordDto);
                if (!response.IsSuccessStatusCode) 
                {
                var messageResponse = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(messageResponse, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                var result = await response.Content.ReadFromJsonAsync<AllRoleToken>();
                DataStorage.RoleName = result!.Name;
                DataStorage.Token = result.Token;
                return true;
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


    }
}
