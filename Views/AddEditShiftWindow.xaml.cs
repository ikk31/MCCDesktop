using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using MCCDesktop.Instruments;
using MCCDesktop.Models.DTOs.Response;
using MCCDesktop.Models.DTOs.Request;

namespace MCCDesktop.Views
{
    public partial class AddEditShiftWindow : Window
    {
        private readonly ApiClient _apiClient;
        private List<AllEmployees> _employees = new List<AllEmployees>();
        private List<AllWorkPlaces> _workplaces = new List<AllWorkPlaces>();
        private int? _shiftId = null;
        private DateTime _selectedDate;

        public bool IsSaved { get; private set; } = false;

        public AddEditShiftWindow(DateTime? selectedDate = null, int? shiftId = null)
        {
            try
            {
                InitializeComponent();
                _apiClient = new ApiClient();
                _shiftId = shiftId;
                _selectedDate = selectedDate ?? DateTime.Today;

                // Устанавливаем события загрузки данных
                Loaded += async (sender, e) =>
                {
                    try
                    {
                        await LoadDataAsync();
                        InitializeForm();

                        if (_shiftId.HasValue)
                        {
                            await LoadShiftDataAsync(_shiftId.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                    }
                };

                InitializeEvents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации окна: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                SetLoadingState(true);

                var employeesTask = _apiClient.GetAllEmployees();
                var workplacesTask = _apiClient.GetAllWorkplaces();

                await Task.WhenAll(employeesTask, workplacesTask);

                _employees = await employeesTask ?? new List<AllEmployees>();
                _workplaces = await workplacesTask ?? new List<AllWorkPlaces>();

                // ОТЛАДКА: Проверяем, что сотрудники загрузились
                Console.WriteLine($"Загружено сотрудников: {_employees.Count}");
                foreach (var emp in _employees.Take(5))
                {
                    Console.WriteLine($"Сотрудник: ID={emp.IdEmployee}, Name={emp.Name}, LastName={emp.LastName}, FullName={emp.FullName}");
                }

                EmployeeComboBox.ItemsSource = _employees.Where(x => x.IsDelete == false);
                EmployeeComboBox.DisplayMemberPath = "FullName"; 

                WorkplaceComboBox.ItemsSource = _workplaces;
                WorkplaceComboBox.DisplayMemberPath = "Name";

                WorkplaceCountText.Text = $"Доступно точек: {_workplaces.Count}";
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки: {ex.Message}");
                Console.WriteLine($"Ошибка загрузки данных: {ex.ToString()}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            SaveButton.IsEnabled = !isLoading;
            SaveButton.Content = isLoading ? "Загрузка..." : "Сохранить";
        }

        private void InitializeForm()
        {
            ShiftDatePicker.SelectedDate = _selectedDate;

            if (!_shiftId.HasValue)
            {
                StartTimeTextBox.Text = "09:00";
                EndTimeTextBox.Text = "18:00";
                BreakHourTextBox.Text = "1";
                BreakMinuteTextBox.Text = "00";
                HourlyRateTextBox.Text = "500";
            }

            CalculateDuration();
            UpdateRemainingChars();
        }

        private void InitializeEvents()
        {
            StartTimeTextBox.TextChanged += TimeTextBox_TextChanged;
            EndTimeTextBox.TextChanged += TimeTextBox_TextChanged;
            BreakHourTextBox.TextChanged += BreakTimeChanged;
            BreakMinuteTextBox.TextChanged += BreakTimeChanged;

            NoBreakCheckBox.Checked += NoBreakChanged;
            NoBreakCheckBox.Unchecked += NoBreakChanged;

            HourlyRateTextBox.TextChanged += RateChanged;
            NotesTextBox.TextChanged += NotesTextChanged;

            BaseRateBtn.Click += BaseRateBtn_Click;
            CustomRateBtn.Click += CustomRateBtn_Click;
            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;

            StartTimeTextBox.PreviewTextInput += TimeTextBox_PreviewTextInput;
            EndTimeTextBox.PreviewTextInput += TimeTextBox_PreviewTextInput;
            BreakHourTextBox.PreviewTextInput += NumberTextBox_PreviewTextInput;
            BreakMinuteTextBox.PreviewTextInput += NumberTextBox_PreviewTextInput;
            HourlyRateTextBox.PreviewTextInput += NumberTextBox_PreviewTextInput;

            StartTimeTextBox.KeyDown += TextBox_KeyDown;
            EndTimeTextBox.KeyDown += TextBox_KeyDown;
            BreakHourTextBox.KeyDown += TextBox_KeyDown;
            BreakMinuteTextBox.KeyDown += TextBox_KeyDown;
            HourlyRateTextBox.KeyDown += TextBox_KeyDown;
            NotesTextBox.KeyDown += TextBox_KeyDown;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender == NotesTextBox)
                {
                    SaveButton_Click(sender, e);
                }
                else
                {
                    MoveFocusToNextControl((Control)sender);
                }
                e.Handled = true;
            }
        }

        private void MoveFocusToNextControl(Control currentControl)
        {
            var controls = new List<Control>
            {
                StartTimeTextBox,
                EndTimeTextBox,
                BreakHourTextBox,
                BreakMinuteTextBox,
                HourlyRateTextBox,
                NotesTextBox
            };

            int currentIndex = controls.IndexOf(currentControl);
            if (currentIndex >= 0 && currentIndex < controls.Count - 1)
            {
                controls[currentIndex + 1].Focus();
            }
        }

        private void TimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            var currentText = textBox.Text;

            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }

            if (currentText.Length == 2 && !currentText.Contains(":"))
            {
                textBox.Text = currentText + ":" + e.Text;
                textBox.CaretIndex = textBox.Text.Length;
                e.Handled = true;
            }

            if (currentText.Length >= 5)
            {
                e.Handled = true;
            }
        }

        private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text;

            if (text.Length == 2 && !text.Contains(":"))
            {
                textBox.Text = text + ":";
                textBox.CaretIndex = 3;
            }

            if (text.Length == 5)
            {
                textBox.Background = IsValidTime(text)
                    ? System.Windows.Media.Brushes.White
                    : System.Windows.Media.Brushes.LightPink;
            }
            else
            {
                textBox.Background = System.Windows.Media.Brushes.White;
            }

            CalculateDuration();
        }

        private bool IsValidTime(string time)
        {
            if (string.IsNullOrWhiteSpace(time) || time.Length != 5 || time[2] != ':')
                return false;

            if (!int.TryParse(time.Substring(0, 2), out int hours) ||
                !int.TryParse(time.Substring(3, 2), out int minutes))
                return false;

            return hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59;
        }

        private TimeSpan ParseTime(string timeText)
        {
            if (IsValidTime(timeText))
            {
                var parts = timeText.Split(':');
                return new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), 0);
            }
            return TimeSpan.Zero;
        }

        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void BreakTimeChanged(object sender, TextChangedEventArgs e)
        {
            CalculateDuration();
        }

        private void NoBreakChanged(object sender, RoutedEventArgs e)
        {
            bool isEnabled = !(NoBreakCheckBox.IsChecked == true);
            BreakHourTextBox.IsEnabled = isEnabled;
            BreakMinuteTextBox.IsEnabled = isEnabled;

            if (!isEnabled)
            {
                BreakHourTextBox.Text = "0";
                BreakMinuteTextBox.Text = "00";
            }

            CalculateDuration();
        }

        private void RateChanged(object sender, TextChangedEventArgs e)
        {
            CalculateDuration();
        }

        private void NotesTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRemainingChars();
        }

        private void CalculateDuration()
        {
            try
            {
                var startTime = ParseTime(StartTimeTextBox.Text);
                var endTime = ParseTime(EndTimeTextBox.Text);

                if (startTime == TimeSpan.Zero || endTime == TimeSpan.Zero)
                    return;

                if (endTime <= startTime)
                {
                    ShowErrorMessage("Время окончания должно быть позже времени начала");
                    return;
                }

                var totalDuration = endTime - startTime;
                TotalTimeText.Text = $"{totalDuration.Hours} ч {totalDuration.Minutes} мин";

                TimeSpan breakTime = TimeSpan.Zero;
                if (!(NoBreakCheckBox.IsChecked == true))
                {
                    if (int.TryParse(BreakHourTextBox.Text, out int breakHours) &&
                        int.TryParse(BreakMinuteTextBox.Text, out int breakMinutes))
                    {
                        breakTime = TimeSpan.FromMinutes(breakHours * 60 + breakMinutes);
                    }
                }
                BreakTimeText.Text = $"{breakTime.Hours} ч {breakTime.Minutes} мин";

                var workedTime = totalDuration - breakTime;
                if (workedTime.TotalHours < 0) workedTime = TimeSpan.Zero;

                double workHours = workedTime.TotalHours;
                WorkedHoursText.Text = $"{workHours:F2} ч";

                if (int.TryParse(HourlyRateTextBox.Text, out int rate))
                {
                    RateText.Text = $"{rate} ₽/ч";
                    var totalAmount = workHours * rate;
                    TotalAmountText.Text = $"{totalAmount:F2} ₽";

                    // Показываем расчет в отладочных целях
                    Console.WriteLine($"Расчет в форме: {workHours:F2} ч * {rate} ₽/ч = {totalAmount:F2} ₽");
                }

                ClearErrorMessage();
            }
            catch
            {
                ShowErrorMessage("Проверьте правильность ввода времени");
            }
        }

        private void UpdateRemainingChars()
        {
            int remaining = 500 - NotesTextBox.Text.Length;
            RemainingCharsText.Text = remaining.ToString();
            RemainingCharsText.Foreground = remaining < 0 ? System.Windows.Media.Brushes.Red :
                                          remaining < 100 ? System.Windows.Media.Brushes.Orange :
                                          System.Windows.Media.Brushes.Green;
        }

        private void ShowErrorMessage(string message)
        {
            ErrorMessageText.Text = message;
        }

        private void ClearErrorMessage()
        {
            ErrorMessageText.Text = "";
        }

        private void BaseRateBtn_Click(object sender, RoutedEventArgs e)
        {
            HourlyRateTextBox.Text = "200";
        }

        private void CustomRateBtn_Click(object sender, RoutedEventArgs e)
        {
            var rateDialog = new RateSelectionDialog();
            if (rateDialog.ShowDialog() == true)
            {
                HourlyRateTextBox.Text = rateDialog.SelectedRate.ToString();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                SetLoadingState(true);

                // Проверяем выбранные элементы
                if (EmployeeComboBox.SelectedItem is not AllEmployees selectedEmployee)
                {
                    ShowErrorMessage("Сотрудник не выбран");
                    return;
                }

                if (WorkplaceComboBox.SelectedItem is not AllWorkPlaces selectedWorkplace)
                {
                    ShowErrorMessage("Рабочая точка не выбрана");
                    return;
                }

                if (!ShiftDatePicker.SelectedDate.HasValue)
                {
                    ShowErrorMessage("Дата не выбрана");
                    return;
                }

                // Парсим время
                var startTime = ParseTime(StartTimeTextBox.Text);
                var endTime = ParseTime(EndTimeTextBox.Text);

                if (startTime == TimeSpan.Zero || endTime == TimeSpan.Zero)
                {
                    ShowErrorMessage("Некорректное время");
                    return;
                }

                // Проверяем, что время окончания позже времени начала
                if (endTime <= startTime)
                {
                    ShowErrorMessage("Время окончания должно быть позже времени начала");
                    EndTimeTextBox.Focus();
                    EndTimeTextBox.SelectAll();
                    return;
                }

                // Рассчитываем общее время
                var totalDuration = endTime - startTime;

                // Рассчитываем перерыв
                int breakDuration = 0;
                if (!(NoBreakCheckBox.IsChecked == true))
                {
                    if (int.TryParse(BreakHourTextBox.Text, out int breakHours) &&
                        int.TryParse(BreakMinuteTextBox.Text, out int breakMinutes))
                    {
                        breakDuration = breakHours * 60 + breakMinutes;
                    }
                }

                var breakTime = TimeSpan.FromMinutes(breakDuration);

                // Рассчитываем фактически отработанные часы
                var workedTime = totalDuration - breakTime;
                if (workedTime.TotalHours < 0)
                {
                    ShowErrorMessage("Перерыв не может быть больше общего времени смены");
                    return;
                }

                double workHours = Math.Round(workedTime.TotalHours, 2);

                // Получаем ставку
                if (!int.TryParse(HourlyRateTextBox.Text, out int hourlyRate) || hourlyRate <= 0)
                {
                    ShowErrorMessage("Некорректная ставка");
                    HourlyRateTextBox.Focus();
                    HourlyRateTextBox.SelectAll();
                    return;
                }

                // Рассчитываем заработок
                decimal totalEarned = Math.Round((decimal)workHours * hourlyRate, 2);

                // Отладочная информация
                Console.WriteLine($"=== РАСЧЕТ ПОЛЕЙ ===");
                Console.WriteLine($"Начало: {startTime}");
                Console.WriteLine($"Окончание: {endTime}");
                Console.WriteLine($"Общее время: {totalDuration}");
                Console.WriteLine($"Перерыв: {breakTime} ({breakDuration} мин)");
                Console.WriteLine($"Отработано часов: {workHours} ч");
                Console.WriteLine($"Ставка: {hourlyRate} ₽/ч");
                Console.WriteLine($"Заработок: {totalEarned} ₽");
                Console.WriteLine($"====================");

                // Создаем объект для отправки
                var addShift = new AddShifts
                {
                    IdShifts = _shiftId ?? 0,
                    IdEmployee = selectedEmployee.IdEmployee,
                    IdWorkplace = selectedWorkplace.IdWorkPlace,
                    Date = DateOnly.FromDateTime(ShiftDatePicker.SelectedDate.Value),
                    StartTime = startTime,
                    EndTime = endTime,
                    HourlyRate = hourlyRate,
                    BreakDuration = breakDuration,
                    Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
                    IsDelete = false,

                    // Добавляем рассчитанные поля
                    WorkHours = workHours,
                    TotalEarned = totalEarned
                };

                bool success = false;

                if (_shiftId.HasValue)
                {
                    // Редактирование существующей смены
                    Console.WriteLine($"Обновляем смену с ID: {_shiftId.Value}");
                    await _apiClient.PutShift(_shiftId.Value, addShift);
                    success = true;
                }
                else
                {
                    // Создание новой смены
                    Console.WriteLine($"Создаем новую смену");
                    await _apiClient.PostShifts(addShift);
                    success = true;
                }

                if (success)
                {
                    IsSaved = true;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка при сохранении: {ex.Message}");
                Console.WriteLine($"ОШИБКА СОХРАНЕНИЯ: {ex.ToString()}");

                MessageBox.Show($"Полная ошибка:\n{ex}", "Ошибка отладки",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private bool ValidateForm()
        {
            if (EmployeeComboBox.SelectedItem == null)
            {
                ShowErrorMessage("Выберите сотрудника");
                EmployeeComboBox.Focus();
                return false;
            }

            if (WorkplaceComboBox.SelectedItem == null)
            {
                ShowErrorMessage("Выберите рабочую точку");
                WorkplaceComboBox.Focus();
                return false;
            }

            if (!ShiftDatePicker.SelectedDate.HasValue)
            {
                ShowErrorMessage("Выберите дату смены");
                ShiftDatePicker.Focus();
                return false;
            }

            if (!IsValidTime(StartTimeTextBox.Text))
            {
                ShowErrorMessage("Введите корректное время начала (ЧЧ:ММ)");
                StartTimeTextBox.Focus();
                StartTimeTextBox.SelectAll();
                return false;
            }

            if (!IsValidTime(EndTimeTextBox.Text))
            {
                ShowErrorMessage("Введите корректное время окончания (ЧЧ:ММ)");
                EndTimeTextBox.Focus();
                EndTimeTextBox.SelectAll();
                return false;
            }

            var startTime = ParseTime(StartTimeTextBox.Text);
            var endTime = ParseTime(EndTimeTextBox.Text);
            if (endTime <= startTime)
            {
                ShowErrorMessage("Время окончания должно быть позже времени начала");
                EndTimeTextBox.Focus();
                EndTimeTextBox.SelectAll();
                return false;
            }

            if (!int.TryParse(HourlyRateTextBox.Text, out int rate) || rate <= 0)
            {
                ShowErrorMessage("Введите корректную ставку");
                HourlyRateTextBox.Focus();
                HourlyRateTextBox.SelectAll();
                return false;
            }

            if (!(NoBreakCheckBox.IsChecked == true))
            {
                if (!int.TryParse(BreakHourTextBox.Text, out int breakHours) || breakHours < 0)
                {
                    ShowErrorMessage("Введите корректное количество часов перерыва");
                    BreakHourTextBox.Focus();
                    BreakHourTextBox.SelectAll();
                    return false;
                }

                if (!int.TryParse(BreakMinuteTextBox.Text, out int breakMinutes) || breakMinutes < 0 || breakMinutes > 59)
                {
                    ShowErrorMessage("Введите корректное количество минут перерыва (0-59)");
                    BreakMinuteTextBox.Focus();
                    BreakMinuteTextBox.SelectAll();
                    return false;
                }
            }

            ClearErrorMessage();
            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async Task LoadShiftDataAsync(int shiftId)
        {
            try
            {
                SetLoadingState(true);

                var shift = await _apiClient.GetShiftById(shiftId);

                if (shift != null)
                {
                    Title = "Редактирование смены";
                    PageTitle.Text = "Редактирование смены";
                    ShiftIdText.Text = $"ID: {shiftId}";
                    ShiftIdText.Visibility = Visibility.Visible;

                    if (shift.Date.HasValue)
                        ShiftDatePicker.SelectedDate = shift.Date.Value.ToDateTime(TimeOnly.MinValue);

                    if (shift.StartTime.HasValue)
                        StartTimeTextBox.Text = shift.StartTime.Value.ToString(@"hh\:mm");

                    if (shift.EndTime.HasValue)
                        EndTimeTextBox.Text = shift.EndTime.Value.ToString(@"hh\:mm");

                    if (shift.BreakDuration.HasValue && shift.BreakDuration > 0)
                    {
                        var breakTime = TimeSpan.FromMinutes(shift.BreakDuration.Value);
                        BreakHourTextBox.Text = breakTime.Hours.ToString();
                        BreakMinuteTextBox.Text = breakTime.Minutes.ToString("D2");
                        NoBreakCheckBox.IsChecked = false;
                    }
                    else
                    {
                        NoBreakCheckBox.IsChecked = true;
                    }

                    if (shift.HourlyRate.HasValue)
                        HourlyRateTextBox.Text = shift.HourlyRate.Value.ToString();

                    NotesTextBox.Text = shift.Notes ?? "";

                    // Показываем рассчитанные поля, если они есть
                    if (shift.WorkHours.HasValue)
                    {
                        Console.WriteLine($"Загружено WorkHours из БД: {shift.WorkHours.Value}");
                    }

                    if (shift.ActualDuration.HasValue) // если у вас есть такое поле
                    {
                        Console.WriteLine($"Загружено ActualDuration из БД: {shift.ActualDuration.Value}");
                    }

                    // Загружаем сотрудника и рабочую точку
                    if (shift.IdEmployee.HasValue && _employees != null)
                    {
                        var employee = _employees.FirstOrDefault(e => e.IdEmployee == shift.IdEmployee.Value);
                        if (employee != null) EmployeeComboBox.SelectedItem = employee;
                    }

                    if (shift.IdWorkplace.HasValue && _workplaces != null)
                    {
                        var workplace = _workplaces.FirstOrDefault(w => w.IdWorkPlace == shift.IdWorkplace.Value);
                        if (workplace != null) WorkplaceComboBox.SelectedItem = workplace;
                    }

                    CalculateDuration();
                }
                else
                {
                    ShowErrorMessage("Смена не найдена");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки данных смены: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private class RateSelectionDialog : Window
        {
            public int SelectedRate { get; private set; } = 500;

            public RateSelectionDialog()
            {
                Title = "Выберите ставку";
                Width = 400;
                Height = 600;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;

                var stack = new StackPanel { Margin = new Thickness(20) };

                var rates = new[] { 180, 190, 200, 205, 215, 220, 250 };
                foreach (var rate in rates)
                {
                    var button = new Button
                    {
                        Content = $"{rate} ₽/час",
                        Margin = new Thickness(0, 0, 0, 5),
                        Padding = new Thickness(10),
                        Tag = rate,
                        Height = 35
                    };

                    button.Click += (s, e) =>
                    {
                        SelectedRate = (int)((Button)s).Tag;
                        DialogResult = true;
                    };

                    stack.Children.Add(button);
                }

                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Margin = new Thickness(0, 10, 0, 0),
                    Padding = new Thickness(10),
                    Height = 35
                };

                cancelButton.Click += (s, e) => DialogResult = false;
                stack.Children.Add(cancelButton);

                Content = stack;
            }
        }
    }
}