using System.Windows.Controls;

namespace MCCDesktop.Views
{
    using MCCDesktop.Instruments;
    using MCCDesktop.Models.DTOs.Request;
    using MCCDesktop.Models.DTOs.Response;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Логика взаимодействия для SalariesPage.xaml
    /// </summary>
    public partial class SalariesPage : Page
    {
        private readonly ApiClient _apiClient;
        private ObservableCollection<AllEmployees> _employees;
        private ObservableCollection<AllShifts> _shifts;
        private ObservableCollection<AllAvans> _avans;

        // Текущие данные
        private AllEmployees _currentEmployee;
        private DateOnly _currentPeriodStart;
        private DateOnly _currentPeriodEnd;
        private CalculationResult _currentResult;

        public class CalculationResult
        {
            public decimal TotalHours { get; set; }
            public decimal TotalEarnings { get; set; }
            public decimal TotalAvans { get; set; }
            public decimal NetAmount { get; set; }
            public int ShiftCount { get; set; }
            public int AvansCount { get; set; }
            public DateOnly PeriodStart { get; set; }
            public DateOnly PeriodEnd { get; set; }
        }

        public SalariesPage()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _employees = new ObservableCollection<AllEmployees>();
            _shifts = new ObservableCollection<AllShifts>();
            _avans = new ObservableCollection<AllAvans>();

            Loaded += SalaryPage_Loaded;
            InitializeEvents();

            // Устанавливаем сегодняшнюю дату в "по"
            EndDatePicker.SelectedDate = DateTime.Today;

            AvansDataGrid.IsReadOnly = false; // Должно быть false
            AvansDataGrid.CanUserAddRows = false;
            AvansDataGrid.CanUserDeleteRows = false;
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

            AddAvansEmployeeCombo.ItemsSource = _employees;
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

        private CalculationResult CalculateTotals()
        {
            var result = new CalculationResult();

            try
            {
                // 1. Рассчитываем по сменам (AllShifts)
                var shiftsInPeriod = _shifts
                    .Where(s => s.Date.HasValue &&
                               s.Date.Value >= _currentPeriodStart &&
                               s.Date.Value <= _currentPeriodEnd)
                    .ToList();

                result.ShiftCount = shiftsInPeriod.Count;

                // Сумма часов (WorkHours имеет тип double?)
                foreach (var shift in shiftsInPeriod)
                {
                    if (shift.WorkHours.HasValue)
                    {
                        result.TotalHours += (decimal)shift.WorkHours.Value;
                    }
                }

                // Общий заработок (TotalEarned имеет тип decimal?)
                foreach (var shift in shiftsInPeriod)
                {
                    if (shift.TotalEarned.HasValue)
                    {
                        result.TotalEarnings += shift.TotalEarned.Value;
                    }
                }

                // 2. Рассчитываем по авансам (AvansDto)
                var avansInPeriod = _avans
                    .Where(a => a.Date >= _currentPeriodStart &&
                               a.Date <= _currentPeriodEnd)
                    .ToList();

                result.AvansCount = avansInPeriod.Count;
                result.TotalAvans = (decimal)avansInPeriod.Sum(a => a.Amount);

                // 3. Рассчитываем чистую сумму к выплате
                result.NetAmount = result.TotalEarnings - result.TotalAvans;

                // 4. Заполняем период
                result.PeriodStart = _currentPeriodStart;
                result.PeriodEnd = _currentPeriodEnd;
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но продолжаем работу
                Debug.WriteLine($"Ошибка расчета итогов: {ex.Message}");

                // Устанавливаем значения по умолчанию
                result.ShiftCount = 0;
                result.TotalHours = 0;
                result.TotalEarnings = 0;
                result.AvansCount = 0;
                result.TotalAvans = 0;
                result.NetAmount = 0;
                result.PeriodStart = _currentPeriodStart;
                result.PeriodEnd = _currentPeriodEnd;
            }

            return result;
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
                await LoadAvans(selectedEmployee.IdEmployee, _currentPeriodStart, _currentPeriodEnd);

                // Рассчитываем итоги
                _currentResult = CalculateTotals();
                _currentResult.PeriodStart = _currentPeriodStart;
                _currentResult.PeriodEnd = _currentPeriodEnd;

                // Обновляем UI
                UpdateCalculationResult();

                StatusText.Text = $"Рассчитано: {_currentResult.ShiftCount} смен, {_currentResult.AvansCount} авансов";
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
                StatusText.Text = $"Загрузка смен за период {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}...";

                // Загружаем смены из API
                var shifts = await _apiClient.GetShiftsByEmployeeAndPeriod(employeeId, startDate, endDate);

                // Очищаем текущую коллекцию
                _shifts.Clear();

                if (shifts != null && shifts.Any())
                {
                    foreach (var shift in shifts)
                    {
                        _shifts.Add(shift); // Добавляем напрямую AllShifts
                    }

                    ShiftsDataGrid.ItemsSource = _shifts;
                    StatusText.Text = $"Загружено {_shifts.Count} смен";
                }
                else
                {
                    ShiftsDataGrid.ItemsSource = null;
                    StatusText.Text = "Смены за выбранный период не найдены";
                }
            }
            catch (HttpRequestException ex)
            {
                StatusText.Text = "Ошибка соединения с сервером";
                MessageBox.Show($"Ошибка загрузки смен: {ex.Message}\nПроверьте подключение к серверу.", "Ошибка сети",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка загрузки смен";
                MessageBox.Show($"Ошибка загрузки смен: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // При загрузке авансов
        private async Task LoadAvans(int employeeId, DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var avansList = await _apiClient.GetAvansByEmployeeAndPeriod(employeeId, startDate, endDate);

                // ОЧИЩАЕМ КОЛЛЕКЦИЮ ПРАВИЛЬНО
                _avans.Clear();

                foreach (var avans in avansList)
                {
                    var dto = new AllAvans
                    {
                        IdAvans = avans.IdAvans,
                        Date = avans.Date,
                        Amount = avans.Amount,
                    };

                    // Подписываемся на изменения для отладки
                    dto.PropertyChanged += (s, e) =>
                    {
                        Console.WriteLine($"Аванс {dto.IdAvans}: {e.PropertyName} изменен");
                    };

                    _avans.Add(dto);
                }

                // Устанавливаем ItemsSource только один раз
                AvansDataGrid.ItemsSource = _avans;

                Console.WriteLine($"Загружено {_avans.Count} авансов");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки авансов: {ex.Message}", "Ошибка");
            }
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
            TotalAvansText.Text = $"{_currentResult.TotalAvans:F2} ₽";
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

                var advanceIds = _avans
                    .Where(a => a.Date >= _currentPeriodStart && a.Date <= _currentPeriodEnd)
                    .Select(a => a.IdAvans)
                    .ToList();

                
                StatusText.Text = "Создание выплаты...";
               
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
            _avans.Clear();
            _currentEmployee = null;
            _currentResult = null;

            // Очищаем UI
            EmployeeInfoText.Text = "";
            PeriodInfoText.Text = "";
            PeriodRangeText.Text = "";
            ShiftCountText.Text = "0";
            TotalHoursText.Text = "";
            TotalEarningsText.Text = "";
            TotalAvansText.Text = "";
            NetAmountText.Text = "";
            PayoutPeriodText.Text = "";
            PeriodNameTextBox.Text = "";
            NotesTextBox.Text = "";
            PaymentDatePicker.SelectedDate = null;

            ShiftsDataGrid.ItemsSource = null;
            AvansDataGrid.ItemsSource = null;

            StatusText.Text = "Выберите сотрудника и период для расчета";
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            //AddEditAvansWindow addEditAvansWindow = new AddEditAvansWindow();
            //addEditAvansWindow.ShowDialog();
        }

        private async void  AddAvansButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка выбора сотрудника
                if (AddAvansEmployeeCombo.SelectedItem is not AllEmployees selectedEmployee)
                {
                    MessageBox.Show("Выберите сотрудника", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка даты
                if (!AddAvansDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату аванса", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка суммы
                if (!decimal.TryParse(AddAvansAmountTextBox.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Введите корректную сумму аванса (больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var avansDto = new AddAvans
                {
                    IdEmployee = selectedEmployee.IdEmployee,
                    Date = DateOnly.FromDateTime(AddAvansDatePicker.SelectedDate.Value),
                    Amount = amount,
                    IsDelete = false
                   
                };

                StatusText.Text = "Создание аванса...";

                // Используем bool версию
                var success = await _apiClient.CreateAvans(avansDto);

                if (success)
                {
                    MessageBox.Show($"✅ Аванс успешно добавлен!\nСумма: {amount:F2} ₽",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем форму
                    ClearAvansForm();

                    // Если текущий сотрудник тот же - обновляем список авансов
                    if (_currentEmployee?.IdEmployee == selectedEmployee.IdEmployee)
                    {
                        await LoadAvans(selectedEmployee.IdEmployee, _currentPeriodStart, _currentPeriodEnd);
                    }

                    StatusText.Text = "Аванс успешно добавлен";
                }
                else
                {
                    MessageBox.Show("Не удалось создать аванс", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания аванса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void ClearAvansFormButton_Click(object sender, RoutedEventArgs e)
        {
            ClearAvansForm();
        }
        private void ClearAvansForm()
        {
            AddAvansDatePicker.SelectedDate = DateTime.Today;
            AddAvansAmountTextBox.Text = "";
            
            StatusText.Text = "Форма очищена";
        }
        private async Task RefreshAvansList()
        {
            try
            {
                if (_currentEmployee == null)
                {
                    StatusText.Text = "Сначала выберите сотрудника";
                    return;
                }

                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    StatusText.Text = "Выберите период";
                    return;
                }

                var startDate = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value);
                var endDate = DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value);

                StatusText.Text = "Обновление списка авансов...";

                // Загружаем авансы заново
                await LoadAvans(_currentEmployee.IdEmployee, startDate, endDate);

                // Обновляем расчет если он был выполнен
                if (_currentResult != null)
                {
                    _currentResult = CalculateTotals();
                    UpdateCalculationResult();
                }

                StatusText.Text = "Список авансов обновлен";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка обновления";
                MessageBox.Show($"Ошибка обновления списка авансов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void DeleteAvansBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AvansDataGrid.SelectedItem is not AllAvans selectedAvans)
                {
                    MessageBox.Show("Выберите аванс для удаления", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить аванс от {selectedAvans.Date:dd.MM.yyyy} " +
                    $"на сумму {selectedAvans.Amount:F2} ₽?\n\n" +
                    "Удаление будет мягким (аванс останется в базе, но будет скрыт).",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Вызываем метод удаления в ApiClient (нужно добавить)
                    var success = await _apiClient.DeleteAvans(selectedAvans.IdAvans);

                    if (success)
                    {
                        MessageBox.Show("✅ Аванс успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем список
                        await RefreshAvansList();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить аванс", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления аванса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        


        private void AvansDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Автоматически не завершаем редактирование - ждем кнопку Сохранить
            e.Cancel = true;
        }

        private void AvansDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // При загрузке строки скрываем кнопку сохранения
            var saveButton = FindVisualChild<Button>(e.Row, "SaveButton");
            if (saveButton != null)
            {
                saveButton.Visibility = Visibility.Collapsed;
            }
        }

        private void AvansAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Валидация ввода суммы
            var textBox = sender as TextBox;
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // Разрешаем цифры, точку и запятую
            if (!decimal.TryParse(newText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                e.Handled = true;
            }
        }

        private async void DeleteAvansButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null || !int.TryParse(button.Tag.ToString(), out int avansId))
                return;

            try
            {
                var avans = _avans.FirstOrDefault(a => a.IdAvans == avansId);
                if (avans == null)
                    return;

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить аванс от {avans.Date:dd.MM.yyyy} " +
                    $"на сумму {avans.Amount:F2} ₽?\n\n" +
                    "Удаление будет мягким (аванс останется в базе, но будет скрыт).",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StatusText.Text = "Удаление аванса...";

                    var success = await _apiClient.DeleteAvans(avansId);

                    if (success)
                    {
                        _avans.Remove(avans);
                        if (_currentEmployee != null && _currentResult != null)
                        {
                            _currentResult = CalculateTotals();
                            UpdateCalculationResult();
                        }

                        StatusText.Text = "Аванс успешно удален";
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить аванс", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательный метод для поиска дочерних элементов
        private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    if (string.IsNullOrEmpty(childName) ||
                        (child is FrameworkElement fe && fe.Name == childName))
                    {
                        return typedChild;
                    }
                }

                var result = FindVisualChild<T>(child, childName);
                if (result != null) return result;
            }

            return null;
        }

        private async void CreatePayoutBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем что расчет выполнен
                if (_currentEmployee == null || _currentResult == null)
                {
                    MessageBox.Show("Сначала выполните расчет зарплаты");
                    return;
                }

                // Получаем ID смен и авансов за период
                var shiftIds = _shifts
                    .Where(s => s.Date >= _currentPeriodStart && s.Date <= _currentPeriodEnd)
                    .Select(s => s.IdShifts)
                    .ToList();

                var avansIds = _avans
                    .Where(a => a.Date >= _currentPeriodStart && a.Date <= _currentPeriodEnd)
                    .Select(a => a.IdAvans)
                    .ToList();

                // Создаем DTO
                var payoutDto = new AddPayout
                {
                    IdEmployee = _currentEmployee.IdEmployee,
                    PeriodStart = _currentPeriodStart,
                    PeriodEnd = _currentPeriodEnd,
                    PeriodName = PeriodNameTextBox.Text.Trim(),
                    TotalAmount = _currentResult.NetAmount,
                    TotalHours = (int)_currentResult.TotalHours,
                    ShiftIds = shiftIds,
                    AvansIds = avansIds,
                    Notes = NotesTextBox.Text,
                    PaidAt = PaymentDatePicker.SelectedDate.HasValue
                        ? DateOnly.FromDateTime(PaymentDatePicker.SelectedDate.Value)
                        : null
                };

                StatusText.Text = "Создание выплаты...";

                // Отправляем запрос
                var payoutId = await _apiClient.CreatePayout(payoutDto);

                if (payoutId)
                {
                    MessageBox.Show($"✅ Выплата успешно создана!\nСумма: {_currentResult.NetAmount:F2} ₽",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем форму
                    ClearForm();
                    StatusText.Text = "Выплата создана успешно";
                }
                else
                {
                    MessageBox.Show("Не удалось создать выплату", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания выплаты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
