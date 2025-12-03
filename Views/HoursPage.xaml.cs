using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Threading.Tasks;
using MCCDesktop.Models.DTOs.Response;
using MCCDesktop.Instruments;

namespace MCCDesktop.Views
{
    public partial class HoursPage : Page
    {
        private readonly ApiClient _apiClient;
        private List<AllShifts> _allShifts = new List<AllShifts>();
        private List<AllEmployees> _employees = new List<AllEmployees>();
        private List<AllShifts> _filteredShifts = new List<AllShifts>();
        private bool _isLoading = false;

        public HoursPage()
        {
            InitializeComponent();
            _apiClient = new ApiClient();

            Loaded += HoursPage_Loaded;
            ApplyFilterBtn.Click += ApplyFilterBtn_Click;
            ExportReportBtn.Click += ExportReportBtn_Click;
            ClearFilterBtn.Click += ClearFilterBtn_Click;
            RefreshBtn.Click += RefreshBtn_Click;
        }

        private async void HoursPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем даты по умолчанию
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Today;

            // Загружаем данные
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;
                LoadingIndicator.Visibility = Visibility.Visible;
                StatusText.Text = "Загрузка данных...";

                // Загружаем данные параллельно
                var shiftsTask = _apiClient.GetAllShifts();
                var employeesTask = _apiClient.GetAllEmployees();

                await Task.WhenAll(shiftsTask, employeesTask);

                _allShifts = await shiftsTask ?? new List<AllShifts>();
                _employees = await employeesTask ?? new List<AllEmployees>();

                // Заполняем комбо-бокс сотрудников
                EmployeeFilter.ItemsSource = _employees;
                EmployeeFilter.DisplayMemberPath = "FullName";

                // Применяем фильтры
                ApplyFilters();

                StatusText.Text = $"Загружено {_allShifts.Count} смен, {_employees.Count} сотрудников";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка загрузки данных";
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                _isLoading = false;
            }
        }

        private void ApplyFilterBtn_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                // Копируем все смены для фильтрации
                _filteredShifts = _allShifts.ToList();

                // Фильтр по дате
                if (StartDatePicker.SelectedDate.HasValue)
                {
                    var startDate = StartDatePicker.SelectedDate.Value.Date;
                    _filteredShifts = _filteredShifts
                        .Where(s => s.Date.HasValue &&
                               DateOnly.FromDateTime(startDate) <= s.Date.Value)
                        .ToList();
                }

                if (EndDatePicker.SelectedDate.HasValue)
                {
                    var endDate = EndDatePicker.SelectedDate.Value.Date;
                    _filteredShifts = _filteredShifts
                        .Where(s => s.Date.HasValue &&
                               DateOnly.FromDateTime(endDate) >= s.Date.Value)
                        .ToList();
                }

                // Фильтр по сотруднику
                if (EmployeeFilter.SelectedItem is AllEmployees selectedEmployee)
                {
                    _filteredShifts = _filteredShifts
                        .Where(s => s.IdEmployee == selectedEmployee.IdEmployee)
                        .ToList();
                }

                // Фильтр по поиску
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    var searchText = SearchTextBox.Text.ToLower();
                    _filteredShifts = _filteredShifts
                        .Where(s =>
                            (s.IdEmployeeNavigation?.Name?.ToLower().Contains(searchText) ?? false) ||
                            (s.IdWorkplaceNavigation?.Name?.ToLower().Contains(searchText) ?? false) ||
                            (s.Notes?.ToLower().Contains(searchText) ?? false))
                        .ToList();
                }

                // Сортировка по дате (сначала новые)
                _filteredShifts = _filteredShifts
                    .OrderByDescending(s => s.Date)
                    .ThenByDescending(s => s.StartTime)
                    .ToList();

                // Обновляем DataGrid
                TimeTrackingGrid.ItemsSource = _filteredShifts;

                // Обновляем статистику
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (_filteredShifts == null || !_filteredShifts.Any())
            {
                RecordCountText.Text = "Найдено записей: 0";
                TotalHoursText.Text = "Всего часов: 0";
                TotalAmountText.Text = "Общая сумма: 0 руб";
                return;
            }

            var recordCount = _filteredShifts.Count;

            // Суммируем отработанные часы (используем WorkHours, если есть, иначе вычисляем)
            var totalHours = _filteredShifts.Sum(s =>
                s.WorkHours ??
                (s.ActualDuration.HasValue ? s.ActualDuration.Value / 60.0 : 0));

            // Суммируем общую сумму (часы * ставка)
            var totalAmount = _filteredShifts.Sum(s =>
                (s.WorkHours ?? (s.ActualDuration.HasValue ? s.ActualDuration.Value / 60.0 : 0)) *
                (s.HourlyRate ?? 0));

            RecordCountText.Text = $"Найдено записей: {recordCount}";
            TotalHoursText.Text = $"Всего часов: {totalHours:F2}";
            TotalAmountText.Text = $"Общая сумма: {totalAmount:F2} руб";
        }

        private void ClearFilterBtn_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Today;
            EmployeeFilter.SelectedItem = null;
            SearchTextBox.Text = string.Empty;
            ApplyFilters();
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void ExportReportBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportReport();
        }

        private void ExportReport()
        {
            try
            {
                if (_filteredShifts == null || !_filteredShifts.Any())
                {
                    MessageBox.Show("Нет данных для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    FileName = $"Отчет_по_сменам_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".csv",
                    AddExtension = true
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var filePath = saveFileDialog.FileName;
                    var extension = Path.GetExtension(filePath).ToLower();

                    switch (extension)
                    {
                        case ".csv":
                            ExportToCsv(filePath);
                            break;
                        case ".txt":
                            ExportToTxt(filePath);
                            break;
                        default:
                            ExportToCsv(filePath);
                            break;
                    }

                    MessageBox.Show($"Отчет успешно сохранен в файл:\n{filePath}", "Экспорт завершен",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            // Заголовок CSV
            writer.WriteLine("Сотрудник;Дата;Рабочая точка;Начало;Окончание;Перерыв;Отработано часов;Ставка (руб/ч);Сумма (руб);Комментарий");

            // Данные
            foreach (var shift in _filteredShifts)
            {
                var employeeName = shift.IdEmployeeNavigation?.Name ?? "Неизвестный сотрудник";
                var workplaceName = shift.IdWorkplaceNavigation?.Name ?? "Не указано";

                // Вычисляем данные
                var workHours = shift.WorkHours ??
                               (shift.ActualDuration.HasValue ? shift.ActualDuration.Value / 60.0 : 0);
                var hourlyRate = shift.HourlyRate ?? 0;
                var totalAmount = workHours * hourlyRate;

                var startTime = shift.StartTime?.ToString(@"hh\:mm") ?? "--:--";
                var endTime = shift.EndTime?.ToString(@"hh\:mm") ?? "--:--";
                var breakDuration = shift.BreakDuration.HasValue ?
                    TimeSpan.FromMinutes(shift.BreakDuration.Value).ToString(@"hh\:mm") : "00:00";

                // Формируем строку
                var line = string.Join(";",
                    EscapeCsvField(employeeName),
                    shift.Date?.ToString("dd.MM.yyyy") ?? "",
                    EscapeCsvField(workplaceName),
                    startTime,
                    endTime,
                    breakDuration,
                    workHours.ToString("F2").Replace(",", "."),
                    hourlyRate.ToString(),
                    totalAmount.ToString("F2").Replace(",", "."),
                    EscapeCsvField(shift.Notes ?? "")
                );

                writer.WriteLine(line);
            }
        }

        private void ExportToTxt(string filePath)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            // Заголовок отчета
            writer.WriteLine("ОТЧЕТ ПО УЧЕТУ РАБОЧЕГО ВРЕМЕНИ");
            writer.WriteLine(new string('=', 60));

            // Период отчета
            var startDate = StartDatePicker.SelectedDate?.ToString("dd.MM.yyyy") ?? "не указано";
            var endDate = EndDatePicker.SelectedDate?.ToString("dd.MM.yyyy") ?? "не указано";
            writer.WriteLine($"Период: {startDate} - {endDate}");

            // Сотрудник (если выбран)
            if (EmployeeFilter.SelectedItem is AllEmployees selectedEmployee)
            {
                writer.WriteLine($"Сотрудник: {selectedEmployee.FullName}");
            }

            writer.WriteLine(new string('-', 60));
            writer.WriteLine();

            // Статистика
            var totalHours = _filteredShifts.Sum(s =>
                s.WorkHours ??
                (s.ActualDuration.HasValue ? s.ActualDuration.Value / 60.0 : 0));
            var totalAmount = _filteredShifts.Sum(s =>
                (s.WorkHours ?? (s.ActualDuration.HasValue ? s.ActualDuration.Value / 60.0 : 0)) *
                (s.HourlyRate ?? 0));

            writer.WriteLine($"Всего записей: {_filteredShifts.Count}");
            writer.WriteLine($"Общее количество часов: {totalHours:F2}");
            writer.WriteLine($"Общая сумма: {totalAmount:F2} руб");
            writer.WriteLine();
            writer.WriteLine(new string('=', 60));
            writer.WriteLine();

            // Таблица данных
            writer.WriteLine("Детализация смен:");
            writer.WriteLine(new string('-', 90));

            // Шапка таблицы
            writer.WriteLine(string.Format("{0,-20} {1,-10} {2,-15} {3,-8} {4,-8} {5,-8} {6,-8} {7,-10}",
                "Сотрудник", "Дата", "Рабочая точка", "Начало", "Окончание", "Часы", "Ставка", "Сумма"));
            writer.WriteLine(new string('-', 90));

            // Данные таблицы
            foreach (var shift in _filteredShifts)
            {
                var employeeName = shift.IdEmployeeNavigation?.Name ?? "Неизвестно";
                var workplaceName = shift.IdWorkplaceNavigation?.Name ?? "Не указано";
                var workHours = shift.WorkHours ??
                               (shift.ActualDuration.HasValue ? shift.ActualDuration.Value / 60.0 : 0);
                var hourlyRate = shift.HourlyRate ?? 0;
                var total = workHours * hourlyRate;
                var startTime = shift.StartTime?.ToString(@"hh\:mm") ?? "--:--";
                var endTime = shift.EndTime?.ToString(@"hh\:mm") ?? "--:--";

                writer.WriteLine(string.Format("{0,-20} {1,-10} {2,-15} {3,-8} {4,-8} {5,-8:F2} {6,-8} {7,-10:F2}",
                    Truncate(employeeName, 19),
                    shift.Date?.ToString("dd.MM.yyyy") ?? "",
                    Truncate(workplaceName, 14),
                    startTime,
                    endTime,
                    workHours,
                    hourlyRate,
                    total));
            }

            writer.WriteLine(new string('-', 90));
            writer.WriteLine();
            writer.WriteLine($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";

            // Если поле содержит кавычки, точку с запятой или перенос строки, заключаем в кавычки
            if (field.Contains("\"") || field.Contains(";") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }

        // Быстрые фильтры по дате
        private void QuickFilterToday_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
            ApplyFilters();
        }

        private void QuickFilterWeek_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            EndDatePicker.SelectedDate = DateTime.Today;
            ApplyFilters();
        }

        private void QuickFilterMonth_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Today;
            ApplyFilters();
        }

        // Поиск при вводе текста (с задержкой)
        private System.Threading.Timer _searchTimer;
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Отменяем предыдущий таймер
            _searchTimer?.Dispose();

            // Запускаем новый таймер с задержкой 500мс
            _searchTimer = new System.Threading.Timer(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    ApplyFilters();
                });
            }, null, 500, System.Threading.Timeout.Infinite);
        }

        // Метод для получения полного имени сотрудника
        private string GetEmployeeFullName(AllEmployees employee)
        {
            if (employee == null) return "";

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(employee.Name)) parts.Add(employee.Name);
            if (!string.IsNullOrEmpty(employee.LastName)) parts.Add(employee.LastName);

            return string.Join(" ", parts);
        }
    }
}