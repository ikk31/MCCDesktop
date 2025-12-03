using System.Windows.Controls;

namespace MCCDesktop.Views
{
    using MCCDesktop.Instruments;
    using MCCDesktop.Models.DTOs.Response;
    using System.Collections.ObjectModel;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Логика взаимодействия для SalariesPage.xaml
    /// </summary>
    public partial class SalariesPage : Page
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<AllEmployees> _employees;
        private ObservableCollection<ShiftDto> _shifts;
        private ObservableCollection<AdvanceDto> _advances;

        // Текущие данные
        private AllEmployees _currentEmployee;
        private DateOnly _currentPeriodStart;
        private DateOnly _currentPeriodEnd;
        private CalculationResult _currentResult;

        public class ShiftDto
        {
            public int IdShifts { get; set; }
            public DateOnly Date { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public double WorkHours { get; set; }
            public int HourlyRate { get; set; }
            public decimal TotalEarned { get; set; }
            public string WorkplaceName { get; set; }
            public string Notes { get; set; }
        }

        public class AdvanceDto
        {
            public int IdAdvance { get; set; }
            public DateOnly AdvanceDate { get; set; }
            public decimal Amount { get; set; }
            public string Notes { get; set; }
        }

        public class CalculationResult
        {
            public decimal TotalHours { get; set; }
            public decimal TotalEarnings { get; set; }
            public decimal TotalAdvances { get; set; }
            public decimal NetAmount { get; set; }
            public int ShiftCount { get; set; }
            public int AdvanceCount { get; set; }
            public DateOnly PeriodStart { get; set; }
            public DateOnly PeriodEnd { get; set; }
        }

        public SalariesPage()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _employees = new ObservableCollection<AllEmployees>();
            _shifts = new ObservableCollection<ShiftDto>();
            _advances = new ObservableCollection<AdvanceDto>();

            Loaded += SalaryPage_Loaded;
            InitializeEvents();

            // Устанавливаем сегодняшнюю дату в "по"
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private void InitializeEvents()
        {
            CalculateBtn.Click += async (s, e) => await CalculateSalary();
            CreatePayoutBtn.Click += async (s, e) => await CreatePayout();
            RefreshBtn.Click += async (s, e) => await LoadEmployees();
        }

        private void SetDefaultDates()
        {
            // По умолчанию: начало месяца - сегодня
            var today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = today;
        }

        // ============ ОБРАБОТЧИКИ КНОПОК БЫСТРОГО ВЫБОРА ============

        private void TodayBtn_Click(object sender, RoutedEventArgs e)
        {
            // Сегодняшний день
            var today = DateTime.Today;
            StartDatePicker.SelectedDate = today;
            EndDatePicker.SelectedDate = today;
            StatusText.Text = "Выбран сегодняшний день";
        }

        private void YesterdayBtn_Click(object sender, RoutedEventArgs e)
        {
            // Вчерашний день
            var yesterday = DateTime.Today.AddDays(-1);
            StartDatePicker.SelectedDate = yesterday;
            EndDatePicker.SelectedDate = yesterday;
            StatusText.Text = "Выбран вчерашний день";
        }

        private void ThisMonthBtn_Click(object sender, RoutedEventArgs e)
        {
            // Весь текущий месяц
            var today = DateTime.Today;
            StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
            EndDatePicker.SelectedDate = today; // До сегодня включительно
            StatusText.Text = "Выбран период с начала месяца по сегодня";
        }

        private void LastMonthBtn_Click(object sender, RoutedEventArgs e)
        {
            // Весь прошлый месяц
            var lastMonth = DateTime.Today.AddMonths(-1);
            StartDatePicker.SelectedDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            EndDatePicker.SelectedDate = new DateTime(lastMonth.Year, lastMonth.Month,
                DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
            StatusText.Text = "Выбран весь прошлый месяц";
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Если начальная дата позже конечной, меняем конечную
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                if (StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
                {
                    EndDatePicker.SelectedDate = StartDatePicker.SelectedDate.Value;
                }
            }
        }

        private async void SalaryPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadEmployees();
            SetDefaultDates();
        }

        private async Task LoadEmployees()
        {
            try
            {
                StatusText.Text = "Загрузка сотрудников...";
                var employees = await _apiClient.GetAllEmployees();
                _employees.Clear();

                foreach (var emp in employees.Where(e => e.IsDelete != true))
                {
                    _employees.Add(emp);
                }

                EmployeeComboBox.ItemsSource = _employees;
                StatusText.Text = $"Загружено сотрудников: {_employees.Count}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка загрузки сотрудников";
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CalculateSalary()
        {
            try
            {
                // Проверяем выбранного сотрудника
                if (EmployeeComboBox.SelectedItem is not AllEmployees selectedEmployee)
                {
                    MessageBox.Show("Выберите сотрудника", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем выбранный период
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите период", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var startDate = StartDatePicker.SelectedDate.Value;
                var endDate = EndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    MessageBox.Show("Дата начала не может быть позже даты окончания", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Сохраняем текущие параметры
                _currentEmployee = selectedEmployee;
                _currentPeriodStart = DateOnly.FromDateTime(startDate);
                _currentPeriodEnd = DateOnly.FromDateTime(endDate);

                // Показываем статус
                StatusText.Text = $"Расчет зарплаты за период {_currentPeriodStart:dd.MM.yyyy} - {_currentPeriodEnd:dd.MM.yyyy}...";

                // Загружаем смены за период (ВКЛЮЧИТЕЛЬНО конечную дату)
                await LoadShifts(selectedEmployee.IdEmployee, _currentPeriodStart, _currentPeriodEnd);

                // Загружаем авансы за период (ВКЛЮЧИТЕЛЬНО конечную дату)
                await LoadAdvances(selectedEmployee.IdEmployee, _currentPeriodStart, _currentPeriodEnd);

                // Рассчитываем итоги
                _currentResult = CalculateTotals();
                _currentResult.PeriodStart = _currentPeriodStart;
                _currentResult.PeriodEnd = _currentPeriodEnd;

                // Обновляем UI
                UpdateCalculationResult();

                StatusText.Text = $"Рассчитано: {_currentResult.ShiftCount} смен, {_currentResult.AdvanceCount} авансов";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка расчета";
                MessageBox.Show($"Ошибка расчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadShifts(int employeeId, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                // ВАЖНО: используем ВКЛЮЧИТЕЛЬНО endDate
                // Ваш endpoint должен учитывать: WHERE Date >= @startDate AND Date <= @endDate

                // TODO: Замените на ваш реальный endpoint
                // var shifts = await _apiClient.GetShiftsByEmployeeAndPeriod(employeeId, startDate, endDate);

                // Пока используем тестовые данные
                _shifts.Clear();

                // Генерируем тестовые смены в выбранном периоде
                var days = (endDate.DayNumber - startDate.DayNumber) + 1;

                for (int i = 0; i < Math.Min(days, 10); i++) // Макс 10 смен для теста
                {
                    var shiftDate = startDate.AddDays(i);

                    _shifts.Add(new ShiftDto
                    {
                        IdShifts = i + 1,
                        Date = shiftDate,
                        StartTime = TimeSpan.FromHours(9),
                        EndTime = TimeSpan.FromHours(18),
                        WorkHours = 8.5,
                        HourlyRate = 500 + (i * 10),
                        TotalEarned = (8.5m * (500 + (i * 10))),
                        WorkplaceName = i % 2 == 0 ? "Кафе Центральное" : "Кафе Парковое",
                        Notes = i == 0 ? "Первая смена" : $"Смена #{i + 1}"
                    });
                }

                ShiftsDataGrid.ItemsSource = _shifts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки смен: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadAdvances(int employeeId, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                // ВАЖНО: используем ВКЛЮЧИТЕЛЬНО endDate
                // Ваш endpoint должен учитывать: WHERE AdvanceDate >= @startDate AND AdvanceDate <= @endDate

                // TODO: Замените на ваш реальный endpoint
                // var advances = await _apiClient.GetAdvancesByEmployeeAndPeriod(employeeId, startDate, endDate);

                // Пока используем тестовые данные
                _advances.Clear();

                // Генерируем тестовые авансы
                var random = new Random();
                var days = (endDate.DayNumber - startDate.DayNumber) + 1;

                for (int i = 0; i < Math.Min(days / 7, 3); i++) // Примерно 3 аванса
                {
                    var advanceDate = startDate.AddDays(i * 7);
                    if (advanceDate > endDate) break;

                    _advances.Add(new AdvanceDto
                    {
                        IdAdvance = i + 1,
                        AdvanceDate = advanceDate,
                        Amount = random.Next(5000, 15000),
                        Notes = $"Аванс #{i + 1}"
                    });
                }

                AdvancesDataGrid.ItemsSource = _advances;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки авансов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private CalculationResult CalculateTotals()
        {
            var result = new CalculationResult();

            // Суммируем смены (только те, что входят в период)
            result.TotalHours = (decimal)_shifts
                .Where(s => s.Date >= _currentPeriodStart && s.Date <= _currentPeriodEnd)
                .Sum(s => s.WorkHours);

            result.TotalEarnings = _shifts
                .Where(s => s.Date >= _currentPeriodStart && s.Date <= _currentPeriodEnd)
                .Sum(s => s.TotalEarned);

            result.ShiftCount = _shifts
                .Count(s => s.Date >= _currentPeriodStart && s.Date <= _currentPeriodEnd);

            // Суммируем авансы (только те, что входят в период)
            result.TotalAdvances = _advances
                .Where(a => a.AdvanceDate >= _currentPeriodStart && a.AdvanceDate <= _currentPeriodEnd)
                .Sum(a => a.Amount);

            result.AdvanceCount = _advances
                .Count(a => a.AdvanceDate >= _currentPeriodStart && a.AdvanceDate <= _currentPeriodEnd);

            // Рассчитываем чистую сумму
            result.NetAmount = result.TotalEarnings - result.TotalAdvances;

            return result;
        }

        private void UpdateCalculationResult()
        {
            if (_currentEmployee == null || _currentResult == null) return;

            // Обновляем информацию
            EmployeeInfoText.Text = $"{_currentEmployee.FullName}";
            PeriodInfoText.Text = $"Период: {_currentPeriodStart:dd.MM.yyyy} - {_currentPeriodEnd:dd.MM.yyyy}";
            PeriodRangeText.Text = $"(включительно с {_currentPeriodStart:dd.MM.yyyy} по {_currentPeriodEnd:dd.MM.yyyy})";

            // Обновляем статистику
            ShiftCountText.Text = _currentResult.ShiftCount.ToString();
            TotalHoursText.Text = $"{_currentResult.TotalHours:F2} ч";
            TotalEarningsText.Text = $"{_currentResult.TotalEarnings:F2} ₽";
            TotalAdvancesText.Text = $"{_currentResult.TotalAdvances:F2} ₽";
            NetAmountText.Text = $"{_currentResult.NetAmount:F2} ₽";

            // Обновляем информацию для создания выплаты
            PayoutPeriodText.Text = $"{_currentPeriodStart:dd.MM.yyyy} - {_currentPeriodEnd:dd.MM.yyyy}";

            // Автозаполняем название периода
            PeriodNameTextBox.Text = $"{_currentEmployee.FullName} - {_currentPeriodStart:MMMM yyyy}";
        }

        private async Task CreatePayout()
        {
            try
            {
                // Проверяем, что был выполнен расчет
                if (_currentEmployee == null || _currentResult == null)
                {
                    MessageBox.Show("Сначала выполните расчет зарплаты", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем название периода
                if (string.IsNullOrWhiteSpace(PeriodNameTextBox.Text))
                {
                    MessageBox.Show("Введите название периода", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем ID смен и авансов для привязки
                var shiftIds = _shifts
                    .Where(s => s.Date >= _currentPeriodStart && s.Date <= _currentPeriodEnd)
                    .Select(s => s.IdShifts)
                    .ToList();

                var advanceIds = _advances
                    .Where(a => a.AdvanceDate >= _currentPeriodStart && a.AdvanceDate <= _currentPeriodEnd)
                    .Select(a => a.IdAdvance)
                    .ToList();

                // Создаем DTO для запроса
                //var payoutDto = new CreatePayoutWithLinksDto
                //{
                //    IdEmployee = _currentEmployee.IdEmployee,
                //    PeriodStart = _currentPeriodStart,
                //    PeriodEnd = _currentPeriodEnd,
                //    PeriodName = PeriodNameTextBox.Text.Trim(),
                //    ShiftIds = shiftIds,
                //    AdvanceIds = advanceIds,
                //    Notes = NotesTextBox.Text
                //};

                // Вызываем API для создания выплаты
                StatusText.Text = "Создание выплаты...";
                //var payoutId = await _apiClient.CreatePayoutWithLinks(payoutDto);

                //if (payoutId.HasValue)
                //{
                //    // Если указана дата выплаты - отмечаем как выплаченную
                //    if (PaymentDatePicker.SelectedDate.HasValue)
                //    {
                //        await _apiClient.MarkPayoutAsPaid(payoutId.Value,
                //            DateOnly.FromDateTime(PaymentDatePicker.SelectedDate.Value));
                //    }

                //    MessageBox.Show($"✅ Выплата успешно создана!\nID: {payoutId}\nСумма: {_currentResult.NetAmount:F2} ₽", "Успех",
                //        MessageBoxButton.OK, MessageBoxImage.Information);

                //    // Очищаем форму
                //    ClearForm();

                //    StatusText.Text = "Выплата создана успешно";
                //}
                //else
                //{
                //    MessageBox.Show("Не удалось создать выплату", "Ошибка",
                //        MessageBoxButton.OK, MessageBoxImage.Error);
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания выплаты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            // Очищаем данные расчета
            _shifts.Clear();
            _advances.Clear();
            _currentEmployee = null;
            _currentResult = null;

            // Очищаем UI
            EmployeeInfoText.Text = "";
            PeriodInfoText.Text = "";
            PeriodRangeText.Text = "";
            ShiftCountText.Text = "0";
            TotalHoursText.Text = "";
            TotalEarningsText.Text = "";
            TotalAdvancesText.Text = "";
            NetAmountText.Text = "";
            PayoutPeriodText.Text = "";
            PeriodNameTextBox.Text = "";
            NotesTextBox.Text = "";
            PaymentDatePicker.SelectedDate = null;

            ShiftsDataGrid.ItemsSource = null;
            AdvancesDataGrid.ItemsSource = null;

            StatusText.Text = "Выберите сотрудника и период для расчета";
        }
    }
}
