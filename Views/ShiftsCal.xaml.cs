using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MCCDesktop.Instruments;
using MCCDesktop.Models.DTOs.Response;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MCCDesktop.Views
{
    public partial class ShiftsCal : Page
    {
        private readonly ApiClient _apiClient;
        private DateTime _currentMonth;
        private List<AllShifts> _allShifts = new List<AllShifts>();
        private DateTime? _selectedDate = null;

        // Классы для отображения данных
        public class CalendarDay
        {
            public int Day { get; set; }
            public DateTime Date { get; set; }
            public bool IsEmpty { get; set; }
            public bool IsToday { get; set; }
            public bool HasShifts { get; set; }
        }

        public class ShiftDisplayItem
        {
            public int ShiftId { get; set; }
            public string EmployeeName { get; set; }
            public string TimeRange { get; set; }
            public double WorkHours { get; set; }
            public int? HourlyRate { get; set; }
            public string Notes { get; set; }
            public DateTime Date { get; set; }
        }

        public class WorkplaceShiftGroup
        {
            public string WorkplaceName { get; set; }
            public int WorkplaceId { get; set; }
            public ObservableCollection<ShiftDisplayItem> Shifts { get; set; } = new ObservableCollection<ShiftDisplayItem>();
        }

        public ShiftsCal()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _currentMonth = DateTime.Today;

            // Инициализация событий
            Loaded += ShiftsCal_Loaded;
            PrevMonthBtn.Click += PrevMonthBtn_Click;
            NextMonthBtn.Click += NextMonthBtn_Click;
            AddShiftBtn.Click += AddShiftBtn_Click;
        }

        private async void ShiftsCal_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }

        private async Task RefreshData()
        {
            try
            {
                // Загружаем свежие данные
                await LoadDataAsync();

                // Обновляем календарь
                GenerateCalendarDays();

                // Если панель информации о дне видна, обновляем её содержимое
                if (DayInfoPanel.Visibility == Visibility.Visible && _selectedDate.HasValue)
                {
                    ShowDayInfo(_selectedDate.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allShifts = await _apiClient.GetAllShifts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки смен: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateCalendarDays()
        {
            MonthYearText.Text = _currentMonth.ToString("MMMM yyyy");

            var days = new List<CalendarDay>();

            // Первый день месяца
            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            // День недели первого дня (0-воскресенье, 1-понедельник и т.д.)
            int firstDayWeekOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7; // Преобразуем к понедельник=0

            // Пустые дни перед первым днем месяца
            for (int i = 0; i < firstDayWeekOffset; i++)
            {
                days.Add(new CalendarDay { IsEmpty = true });
            }

            // Дни месяца
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var today = DateTime.Today;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                var hasShifts = _allShifts.Any(s => s.Date.HasValue &&
                    s.Date.Value.ToDateTime(TimeOnly.MinValue).Date == date.Date);

                days.Add(new CalendarDay
                {
                    Day = day,
                    Date = date,
                    IsToday = date.Date == today.Date,
                    HasShifts = hasShifts,
                    IsEmpty = false
                });
            }

            // Добавляем пустые дни до 42 (6 недель)
            while (days.Count < 42)
            {
                days.Add(new CalendarDay { IsEmpty = true });
            }

            CalendarGrid.ItemsSource = days;
        }

        private void PrevMonthBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            GenerateCalendarDays();
        }

        private void NextMonthBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            GenerateCalendarDays();
        }

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CalendarDay day && !day.IsEmpty)
            {
                ShowDayInfo(day.Date);
            }
        }

        private void ShowDayInfo(DateTime date)
        {
            _selectedDate = date;
            DayInfoPanel.Visibility = Visibility.Visible;

            // Обновляем заголовок
            SelectedDateText.Text = date.ToString("dd MMMM yyyy, dddd");

            // Фильтруем смены на выбранную дату
            var dayShifts = _allShifts
                .Where(s => s.Date.HasValue &&
                       s.Date.Value.ToDateTime(TimeOnly.MinValue).Date == date.Date)
                .ToList();

            // Группируем по рабочим точкам
            var groupedShifts = dayShifts
                .GroupBy(s => s.IdWorkplaceNavigation?.Name ?? "Не указана")
                .Select(g => new WorkplaceShiftGroup
                {
                    WorkplaceName = g.Key,
                    WorkplaceId = g.First().IdWorkplace ?? 0,
                    Shifts = new ObservableCollection<ShiftDisplayItem>(g.Select(s => new ShiftDisplayItem
                    {
                        ShiftId = s.IdShifts,
                        EmployeeName = s.IdEmployeeNavigation?.Name ?? "Не указан",
                        TimeRange = $"{s.StartTime?.ToString(@"hh\:mm") ?? "--:--"} - {s.EndTime?.ToString(@"hh\:mm") ?? "--:--"}",
                        WorkHours = s.WorkHours ?? 0,
                        HourlyRate = s.HourlyRate,
                        Notes = s.Notes ?? "",
                        Date = s.Date?.ToDateTime(TimeOnly.MinValue) ?? date
                    }))
                })
                .ToList();

            // Обновляем счетчик смен
            ShiftCountText.Text = $"{dayShifts.Count} смен";

            // Устанавливаем источник данных
            ShiftsList.ItemsSource = groupedShifts;
        }

        private void HideDayInfoPanel()
        {
            DayInfoPanel.Visibility = Visibility.Collapsed;
            _selectedDate = null;
        }

        // ДОБАВЛЯЕМ ЭТОТ МЕТОД - обработчик кнопки редактирования
        private void EditShift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ShiftDisplayItem shiftItem)
                {
                    // Создаем окно редактирования
                    var editWindow = new AddEditShiftWindow(shiftItem.Date, shiftItem.ShiftId);

                    // Настраиваем окно
                    editWindow.Owner = Window.GetWindow(this);
                    editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Показываем как диалоговое окно
                    bool? result = editWindow.ShowDialog();

                    // Если изменения были сохранены, обновляем данные
                    if (result == true || editWindow.IsSaved)
                    {
                        // Используем Dispatcher для обновления UI из другого потока
                        Dispatcher.Invoke(async () =>
                        {
                            await RefreshData();
                        });
                    }
                }
                else
                {
                    MessageBox.Show("Не удалось получить информацию о смене для редактирования",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна редактирования: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки удаления
        private async void DeleteShift_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ShiftDisplayItem shiftItem)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить смену сотрудника {shiftItem.EmployeeName}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // TODO: Реализовать метод удаления в ApiClient
                        // await _apiClient.DeleteShift(shiftItem.ShiftId);
                        MessageBox.Show("Смена удалена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await RefreshData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Обработчик кнопки добавления смены
        private void AddShiftBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedDate.HasValue)
                {
                    // Создаем окно добавления смены
                    var addWindow = new AddEditShiftWindow(_selectedDate.Value);

                    // Настраиваем окно
                    addWindow.Owner = Window.GetWindow(this);
                    addWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Показываем как диалоговое окно
                    bool? result = addWindow.ShowDialog();

                    // Если смена была добавлена, обновляем данные
                    if (result == true || addWindow.IsSaved)
                    {
                        Dispatcher.Invoke(async () =>
                        {
                            await RefreshData();
                        });
                    }
                }
                else
                {
                    MessageBox.Show("Сначала выберите день в календаре",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна добавления: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}