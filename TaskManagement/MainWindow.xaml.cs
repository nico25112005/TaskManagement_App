using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Windows.Threading;

namespace TaskManagement
{
    public partial class MainWindow : Window
    {
        private TaskViewModel _draggedTask;
        private TimerViewModel _timer = new();

        // Todo filter/sort state
        private string _todoFilterText = "";
        private int _todoSortIndex = 0;

        // Drag-and-drop visual feedback state
        private readonly Brush _dragHoverHighlightBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xEF, 0xD9)); // #FFEFD9 (pale yellow)
        private readonly Brush _dropFlashBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC9, 0x41));        // #FFC941 (yellow)
        private Brush? _dragHoverOriginalBackground;
        private DispatcherTimer? _dropFlashTimer;

        // Plan view: currently displayed week. Always anchored to Monday.
        private DateTime _planStartDay = DateTime.Today;

        // Plan view: the inner Grid that holds the 7 day-columns.
        // Kept as field so RefreshPlanView can rebuild it without XAML.
        private Grid? _planGrid;

        public MainWindow()
        {
            InitializeComponent();
            _timer.PropertyChanged += (s, e) =>
            {
                TimerDisplay.Content = _timer.Display;
                TimerState.Content = _timer.StateLabel;
                BtnTimerStart.IsEnabled = !_timer.IsRunning;
                BtnTimerPause.IsEnabled = _timer.IsRunning;
                BtnTimerStop.IsEnabled = _timer.IsRunning;
                BtnTimerSkip.IsEnabled = _timer.Session.IsOnBreak;
            };
            Settings.Load();
            StartUp();
            SetColors();
            InitPlanView();
            InitSettingsUi();
        }

        private void StartUp()
        {
            try
            {
                Tasks.ReadDataFromJson<Dictionary<string, Task>>("todos", out Tasks.tasks);
                Tasks.ReadDataFromJson<Dictionary<string, Task>>("done", out Tasks.done);
                Tasks.tasks ??= new Dictionary<string, Task>();
                Tasks.done ??= new Dictionary<string, Task>();
                CalendarEvents.ReadDataFromJson();

                //Tasks.GenerateTasks(6, 1, 5, 1, 3, false);
                TaskSorter.Distributor();
                RefreshTodoList();
                RefreshWeekPlan();
                RefreshFocusStats();

                // Default create-form values for first-time users
                if (Dp_delivery_date != null)
                    Dp_delivery_date.SelectedDate = DateTime.Today.AddDays(7);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Fehler bei StartUp: {ex.Message}");
            }
        }

        private void RefreshFocusStats()
        {
            try
            {
                var statsPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "focus_stats.json");
                if (!System.IO.File.Exists(statsPath))
                {
                    Lbl_focusStats.Text = "Focus stats: no completed blocks yet \u2014 start the timer to begin.";
                    return;
                }
                var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<FocusStats>(System.IO.File.ReadAllText(statsPath));
                if (stats == null)
                {
                    Lbl_focusStats.Text = "";
                    return;
                }
                // Auto-reset today's counter if the file is from a previous day
                if (stats.LastDay.Date != DateTime.Today)
                {
                    stats.LastDay = DateTime.Today;
                    stats.TodayBlocks = 0;
                }
                Lbl_focusStats.Text = $"Today: {stats.TodayBlocks} focus block{(stats.TodayBlocks == 1 ? "" : "s")} \u2014 Lifetime: {stats.TotalBlocks}";
            }
            catch (Exception ex)
            {
                Lbl_focusStats.Text = $"Focus stats unavailable ({ex.Message})";
            }
        }

        private void Create(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                Tasks.tasks.Add(Task.GenerateId(), new Task(float.Parse(Tb_hours.Text), Tb_description.Text, Dp_delivery_date.SelectedDate.GetValueOrDefault(), byte.Parse(Tb_importancy.Text), false));
                Trace.WriteLine($"Neue Aufgabe '{Tb_description.Text}' erstellt.");
                Tasks.WriteDataToJson<Dictionary<string, Task>>("todos", Tasks.tasks); // <string, Task>
                Calculate();

                // Clear the form for the next entry so the user can keep typing.
                Tb_description.Clear();
                Tb_hours.Clear();
                Tb_importancy.Clear();
                Dp_delivery_date.SelectedDate = DateTime.Today.AddDays(7);
                Tb_description.Focus();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Fehler beim Erstellen der Aufgabe: {ex.Message}");
            }
        }

        private void CreateForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Press Enter in any of the create-form text boxes to submit the new task.
            if (e.Key == Key.Enter)
            {
                Create(sender, e);
                e.Handled = true;
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Tb_description.Text) || string.IsNullOrWhiteSpace(Tb_hours.Text) || string.IsNullOrWhiteSpace(Tb_importancy.Text) || Dp_delivery_date.SelectedDate == null)
            {
                Trace.WriteLine("Bitte füllen Sie alle Felder aus.");
                MessageBox.Show("Alle Felder müssen ausgefüllt sein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!float.TryParse(Tb_hours.Text, out _) || !byte.TryParse(Tb_importancy.Text, out _))
            {
                Trace.WriteLine("Ungültige Eingabeformate.");
                MessageBox.Show("Bitte geben Sie gültige numerische Werte für Stunden und Wichtigkeit ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void Calculate()
        {
            try
            {
                Tasks.nWeek.Clear();
                TaskSorter.Distributor();
                RefreshTodoList();
                RefreshWeekPlan();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Fehler bei der Berechnung: {ex.Message}");
            }
        }

        private HomeTodoViewModel _homeTodo = new();

        private void RefreshTodoList()
        {
            // Build the filtered + sorted source for the Todo list.
            var source = Tasks.tasks.Values.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_todoFilterText))
            {
                var filter = _todoFilterText.Trim().ToLowerInvariant();
                source = source.Where(t => t.Description.ToLowerInvariant().Contains(filter));
            }

            source = _todoSortIndex switch
            {
                1 => source.OrderByDescending(t => t.Importance).ThenBy(t => t.Delivery),
                2 => source.OrderByDescending(t => t.Hours),
                3 => source.OrderBy(t => t.Description),
                _ => source.OrderBy(t => t.Delivery)
            };

            // Refresh the Todo (create-new-task) page's full list.
            // Guard against null controls during partial initialization.
            if (Lv_Todos != null)
            {
                bool hasTasks = Tasks.tasks.Count > 0;
                Lv_Todos.Visibility = hasTasks ? Visibility.Visible : Visibility.Collapsed;
                if (Tb_TodoEmpty != null)
                    Tb_TodoEmpty.Visibility = hasTasks ? Visibility.Collapsed : Visibility.Visible;
                Lv_Todos.ItemsSource = null;
                Lv_Todos.ItemsSource = source.ToList();
            }

            // Refresh the Home page's Today/Upcoming buckets via the view-model.
            _homeTodo.Refresh();
            if (Lv_TodayTodos != null)
            {
                bool hasToday = _homeTodo.Today.Count > 0;
                Lv_TodayTodos.Visibility = hasToday ? Visibility.Visible : Visibility.Collapsed;
                if (Tb_TodayEmpty != null)
                    Tb_TodayEmpty.Visibility = hasToday ? Visibility.Collapsed : Visibility.Visible;
                Lv_TodayTodos.ItemsSource = _homeTodo.Today;
            }
            if (Lv_UpcomingTodos != null)
            {
                bool hasUpcoming = _homeTodo.Upcoming.Count > 0;
                Lv_UpcomingTodos.Visibility = hasUpcoming ? Visibility.Visible : Visibility.Collapsed;
                if (Tb_UpcomingEmpty != null)
                    Tb_UpcomingEmpty.Visibility = hasUpcoming ? Visibility.Collapsed : Visibility.Visible;
                Lv_UpcomingTodos.ItemsSource = _homeTodo.Upcoming;
            }
            if (Lv_DoneTodos != null)
                Lv_DoneTodos.ItemsSource = _homeTodo.Done;
            if (TodoLoadLabel != null)
            {
                TodoLoadLabel.Content = _homeTodo.TodayLoadLabel;
                TodoLoadLabel.Foreground = _homeTodo.IsOverloaded ? Brushes.IndianRed : Brushes.Gray;
            }

            RefreshStatusBar();
        }

        /// <summary>
        /// Update the bottom status bar with open tasks, today's load, and focus blocks.
        /// Called whenever the planner data or task list changes.
        /// </summary>
        private void RefreshStatusBar()
        {
            int openTasks = Tasks.tasks.Count;
            float todayHours = Tasks.nWeek.Values
                .Where(w => w.Date.Date == DateTime.Today)
                .Sum(w => w.PlanedHours);

            int todayFocusBlocks = 0;
            try
            {
                var statsPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "focus_stats.json");
                if (System.IO.File.Exists(statsPath))
                {
                    var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<FocusStats>(System.IO.File.ReadAllText(statsPath));
                    if (stats != null && stats.LastDay.Date == DateTime.Today)
                        todayFocusBlocks = stats.TodayBlocks;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"StatusBar focus stats read failed: {ex.Message}");
            }

            Status_OpenTasks.Text = $"Open tasks: {openTasks}";
            Status_TodayHours.Text = $"Today: {todayHours:F1}h";
            Status_FocusBlocks.Text = $"Focus blocks today: {todayFocusBlocks}";

            // Quick health message
            if (_homeTodo.IsOverloaded)
                Status_Message.Text = "Overloaded today — consider moving work to tomorrow.";
            else if (openTasks == 0)
                Status_Message.Text = "All caught up — great job!";
            else
                Status_Message.Text = "Ctrl+Shift+N to capture a new task quickly.";
        }

        private void RefreshWeekPlan()
        {
            var weekPlan = Tasks.nWeek.Select(kvp => new WeekPlanViewModel
            {
                Day = $"{kvp.Value.Date:ddd dd.MM.}",
                DayDate = kvp.Value.Date.Date,
                PlanedHours = kvp.Value.PlanedHours,
                Tasks = kvp.Value.Tasks.Select(task => new TaskViewModel { Description = task.Description, Hours = task.Hours }).ToList()
            }).ToList();

            Lv_WeekPlan.ItemsSource = weekPlan;
        }

        private void WeekPlan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is TaskViewModel task)
            {
                _draggedTask = task;
                DragDrop.DoDragDrop(fe, task, DragDropEffects.Move);
            }
        }

        private void TaskChip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Alias for XAML to keep the handler name short
            WeekPlan_MouseLeftButtonDown(sender, e);
        }

        private void DayTaskContainer_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(TaskViewModel)) ? DragDropEffects.Move : DragDropEffects.None;

            if (sender is not Panel container) return;

            // Remember the original background on first enter so DragLeave can restore it.
            if (_dragHoverOriginalBackground == null)
                _dragHoverOriginalBackground = container.Background;

            container.Background = _dragHoverHighlightBrush;
        }

        private void DayTaskContainer_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is not Panel container) return;

            container.Background = _dragHoverOriginalBackground ?? Brushes.Transparent;
            _dragHoverOriginalBackground = null;
        }

        private void DayTaskContainer_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(TaskViewModel)) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// Flashes the target day container's background from the accent yellow
        /// back to its original color over ~600ms to give the user clear drop feedback.
        /// </summary>
        private void FlashDropTarget(Panel container)
        {
            // Stop any previous flash so we don't stack timers on rapid drops.
            _dropFlashTimer?.Stop();

            var originalBrush = container.Background;
            container.Background = _dropFlashBrush;

            _dropFlashTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600),
                Tag = (container, originalBrush)
            };

            _dropFlashTimer.Tick += (s, e) =>
            {
                var timer = (DispatcherTimer)s;
                timer.Stop();

                if (timer.Tag is ValueTuple<Panel, Brush> flashInfo)
                {
                    var (element, restoreBrush) = flashInfo;
                    element.Background = restoreBrush;
                }

                _dropFlashTimer = null;
            };

            _dropFlashTimer.Start();
        }

        private void DayTaskContainer_Drop(object sender, DragEventArgs e)
        {
            if (_draggedTask == null) return;
            if (sender is not FrameworkElement fe) return;
            if (fe.Tag is not WeekPlanViewModel targetDay) return;

            // Find the source day by description (we don't track chunk IDs in the view-model)
            foreach (var week in Tasks.nWeek.Values)
            {
                var match = week.Tasks.FirstOrDefault(t => t.Description == _draggedTask.Description);
                if (match != null)
                {
                    week.Tasks.Remove(match);
                    week.PlanedHours = Math.Max(0, week.PlanedHours - match.Hours);

                    // Re-append the moved chunk to the target day
                    int targetDayIndex = Tasks.nWeek.First(kvp => kvp.Value.Date.Date == targetDay.DayDate).Key;
                    if (!Tasks.nWeek.ContainsKey(targetDayIndex))
                        Tasks.nWeek[targetDayIndex] = new Week(targetDay.DayDate);
                    Tasks.nWeek[targetDayIndex].Tasks.Add(match);
                    Tasks.nWeek[targetDayIndex].PlanedHours += match.Hours;

                    Trace.WriteLine($"Moved '{match.Description}' to day {targetDayIndex}");
                    break;
                }
            }

            _draggedTask = null;

            // Visual drop feedback: flash the target container before the view refreshes.
            if (sender is Panel dropContainer)
                FlashDropTarget(dropContainer);

            RefreshWeekPlan();
        }

        private Brush? Colorpalet(byte index)
        {
            List<string> hexColors = new()
            {
                "#5E97D9",
                "#e35d48",
                "#FFC941",
                "#509C6E"
            };

            var bc = new BrushConverter();
            return hexColors[index] != null ? bc.ConvertFrom(hexColors[index]) as Brush : Brushes.White;
        }


        private void Cb_eventType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // During InitializeComponent the controls may not be ready yet. Guard against it.
            if (Tb_eventTitle == null) return;

            // Auto-fill "Work" when Work hours is selected, but only if the user hasn't
            // already typed a meaningful title. Don't overwrite real user input.
            if (Cb_eventType.SelectedIndex == (int)CalendarEventType.WorkHours)
            {
                if (string.IsNullOrWhiteSpace(Tb_eventTitle.Text) || Tb_eventTitle.Text == "Work")
                    Tb_eventTitle.Text = "Work";
            }
            else if (Tb_eventTitle.Text == "Work")
            {
                Tb_eventTitle.Clear();
            }
        }

        private void DeleteTodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not Task task) return;
            var id = Tasks.tasks.FirstOrDefault(kvp => kvp.Value == task).Key;
            if (id == null)
                id = Tasks.tasks.FirstOrDefault(kvp => ReferenceEquals(kvp.Value, task)).Key;
            if (id == null)
            {
                Trace.WriteLine($"DeleteTodo_Click: task reference not found in dictionary.");
                return;
            }

            var result = MessageBox.Show(
                $"Delete task '{task.Description}'?",
                "Delete task",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            Tasks.tasks.Remove(id);
            Tasks.WriteDataToJson<Dictionary<string, Task>>("todos", Tasks.tasks);
            Calculate(); // re-distribute + refresh all views
            Trace.WriteLine($"Deleted task '{task.Description}'.");
        }

        private void Tb_todoFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            _todoFilterText = Tb_todoFilter?.Text ?? "";
            RefreshTodoList();
        }

        private void Cb_todoSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _todoSortIndex = Cb_todoSort?.SelectedIndex ?? 0;
            RefreshTodoList();
        }

        private void Lv_Todos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Lv_Todos.SelectedItem is not Task task) return;

            var dlg = new EditTaskWindow(task) { Owner = this };
            var ok = dlg.ShowDialog();
            if (ok == true)
            {
                Tasks.WriteDataToJson<Dictionary<string, Task>>("todos", Tasks.tasks);
                Calculate();
                Trace.WriteLine($"Edited task '{task.Description}'.");
            }
        }

        private void UndoDoneTodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not DoneTaskViewModel doneVm) return;
            if (string.IsNullOrEmpty(doneVm.Id)) return;
            Tasks.MarkUndone(doneVm.Id);
            Calculate();
            Trace.WriteLine($"Restored '{doneVm.Description}' from done.");
        }

        private void SetColors()
        {
            Timer_Label.Background = Colorpalet(0);
            Todo_Label.Background = Colorpalet(2);
            Termine_Label.Background = Colorpalet(1);
            Fertig_Label.Background = Colorpalet(3);

            Description_Label.Background = Colorpalet(0);
            Hours_Label.Background = Colorpalet(1);
            DeliveryDate_Label.Background = Colorpalet(2);
            Importancy_Label.Background = Colorpalet(3);


        }

        enum Pages
        {
            Home,
            Todo,
            Plan,
            Settings

        }

        Pages pages = Pages.Home;

        private void ChangePage(Pages page)
        {
            pages = page;
            RefreshPage();
        }

        private void RefreshPage()
        {
            Dictionary<Pages, UIElement> pageMap = new()
            {
                { Pages.Home, Home },
                { Pages.Todo, Todo },
                { Pages.Plan, Plan },
                { Pages.Settings, SettingsPage }
            };

            foreach (var page in pageMap)
            {
                page.Value.Visibility = page.Key == pages ? Visibility.Visible : Visibility.Collapsed;
            }

            // Update window title to reflect current page so users always know where they are.
            Title = pages switch
            {
                Pages.Home => "Task Management \u2014 Home",
                Pages.Todo => "Task Management \u2014 To-do",
                Pages.Plan => "Task Management \u2014 Plan",
                Pages.Settings => "Task Management \u2014 Settings",
                _ => "Task Management"
            };
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+1..4: page switch
            // Ctrl+N: focus new-task input on Todo page
            // Ctrl+Shift+N: open Quick-Capture overlay (works from any page)
            var mods = Keyboard.Modifiers;

            if (mods == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D1:
                        ChangePage(Pages.Home);
                        e.Handled = true;
                        break;
                    case Key.D2:
                        Todo_page(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.D3:
                        Plan_page(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.D4:
                        Settings_page(this, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Key.N:
                        if (pages == Pages.Todo && Tb_description != null)
                        {
                            Tb_description.Focus();
                            Tb_description.SelectAll();
                            e.Handled = true;
                        }
                        break;
                }
            }
            else if (mods == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.N)
            {
                OpenQuickCapture();
                e.Handled = true;
            }
        }

        private void OpenQuickCapture()
        {
            var dlg = new QuickCaptureWindow { Owner = this };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.CapturedDescription))
            {
                // Sensible defaults: 1h, importance 2 (medium), delivery in 7 days.
                var id = Task.GenerateId();
                var task = new Task(1f, dlg.CapturedDescription, DateTime.Today.AddDays(7), (byte)2, false);
                Tasks.tasks[id] = task;
                Tasks.WriteDataToJson<Dictionary<string, Task>>("todos", Tasks.tasks);
                Calculate();
                Trace.WriteLine($"Quick-captured '{dlg.CapturedDescription}'.");
            }
        }

        private void Home_page(object sender, RoutedEventArgs e)
        {
            ChangePage(Pages.Home);
        }

        private void Todo_page(object sender, RoutedEventArgs e)
        {
            ChangePage(Pages.Todo);
            RefreshTodoList();
        }

        private void Plan_page(object sender, RoutedEventArgs e)
        {
            ChangePage(Pages.Plan);
            RefreshPlanView();
        }

        private void Settings_page(object sender, RoutedEventArgs e)
        {
            ChangePage(Pages.Settings);
            RefreshSettingsUi();
        }

        private void RefreshSettingsUi()
        {
            Sl_maxHours.Value = Settings.maxHoursPerDay;
            Lbl_maxHours.Text = $"{Settings.maxHoursPerDay:F1}h";

            Sl_planableDays.Value = Settings.maxPlanableDays;
            Lbl_planableDays.Text = $"{Settings.maxPlanableDays}d";

            Sl_focusMin.Value = Settings.focusBlockMinutes;
            Lbl_focusMin.Text = $"{Settings.focusBlockMinutes}min";

            Sl_shortBreak.Value = Settings.shortBreakMinutes;
            Lbl_shortBreak.Text = $"{Settings.shortBreakMinutes}min";

            Sl_longBreak.Value = Settings.longBreakMinutes;
            Lbl_longBreak.Text = $"{Settings.longBreakMinutes}min";

            Sl_blocksBeforeLong.Value = Settings.blocksBeforeLongBreak;
            Lbl_blocksBeforeLong.Text = $"{Settings.blocksBeforeLongBreak}";

            Cb_workStart.SelectedIndex = Settings.workStartHour;
            Cb_workEnd.SelectedIndex = Settings.workEndHour;

            Tg_darkMode.IsChecked = Settings.darkMode;
            Tg_darkMode.Content = Settings.darkMode ? "On" : "Off";

            Lbl_dataPath.Text = System.AppContext.BaseDirectory;
            Lbl_version.Text = "Version 0.1 \u2014 in active development";
        }

        private void InitSettingsUi()
        {
            // Populate 0..24 hour pickers for work-day boundaries
            for (int h = 0; h <= 24; h++)
            {
                Cb_workStart.Items.Add($"{h:D2}:00");
                Cb_workEnd.Items.Add($"{h:D2}:00");
            }
            RefreshSettingsUi();
        }

        private void Sl_maxHours_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.maxHoursPerDay = (float)e.NewValue;
            Lbl_maxHours.Text = $"{Settings.maxHoursPerDay:F1}h";
            Settings.Save();
        }

        private void Sl_planableDays_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.maxPlanableDays = (byte)e.NewValue;
            Lbl_planableDays.Text = $"{Settings.maxPlanableDays}d";
            Settings.Save();
        }

        private void Sl_focusMin_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.focusBlockMinutes = (int)e.NewValue;
            Lbl_focusMin.Text = $"{Settings.focusBlockMinutes}min";
            Settings.Save();
        }

        private void Sl_shortBreak_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.shortBreakMinutes = (int)e.NewValue;
            Lbl_shortBreak.Text = $"{Settings.shortBreakMinutes}min";
            Settings.Save();
        }

        private void Sl_longBreak_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.longBreakMinutes = (int)e.NewValue;
            Lbl_longBreak.Text = $"{Settings.longBreakMinutes}min";
            Settings.Save();
        }

        private void Sl_blocksBeforeLong_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.blocksBeforeLongBreak = (int)e.NewValue;
            Lbl_blocksBeforeLong.Text = $"{Settings.blocksBeforeLongBreak}";
            Settings.Save();
        }

        private void Cb_workHours_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Cb_workStart.SelectedIndex < 0 || Cb_workEnd.SelectedIndex < 0) return;
            if (Cb_workEnd.SelectedIndex <= Cb_workStart.SelectedIndex) return;
            Settings.workStartHour = Cb_workStart.SelectedIndex;
            Settings.workEndHour = Cb_workEnd.SelectedIndex;
            Settings.Save();
        }

        private void Tg_darkMode_Changed(object sender, RoutedEventArgs e)
        {
            Settings.darkMode = Tg_darkMode.IsChecked == true;
            Tg_darkMode.Content = Settings.darkMode ? "On" : "Off";
            Settings.Save();
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = System.AppContext.BaseDirectory;
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"OpenDataFolder failed: {ex.Message}");
                Lbl_exportStatus.Text = $"Could not open folder: {ex.Message}";
            }
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = System.AppContext.BaseDirectory;
                var file = System.IO.Path.Combine(folder, $"taskmanagement-export-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                var payload = new
                {
                    exportedAt = DateTime.Now,
                    tasks = Tasks.tasks,
                    calendarEvents = CalendarEvents.events,
                    settings = Settings.ToData()
                };
                System.IO.File.WriteAllText(file, Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented));
                Lbl_exportStatus.Text = $"Exported to {file}";
                Lbl_exportStatus.Foreground = Brushes.DarkGreen;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Export failed: {ex.Message}");
                Lbl_exportStatus.Text = $"Export failed: {ex.Message}";
                Lbl_exportStatus.Foreground = Brushes.IndianRed;
            }
        }

        private void Link_github_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                if (e.Uri == null) return;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex) { Trace.WriteLine($"Link click failed: {ex.Message}"); }
        }

        // === Plan view (calendar / fixed events) ===

        private void InitPlanView()
        {
            // Populate time pickers 0:00 .. 23:00 (start), 1:00 .. 24:00 (end).
            // Hour-only granularity for v1 – minute-precision is a nice-to-have.
            for (int h = 0; h < 24; h++)
            {
                Cb_eventStart.Items.Add($"{h:D2}:00");
                Cb_eventEnd.Items.Add($"{h + 1:D2}:00");
            }
            Cb_eventStart.SelectedIndex = 9;   // 09:00
            Cb_eventEnd.SelectedIndex = 17;    // 18:00
            Dp_eventDay.SelectedDate = DateTime.Today;

            // Reference the calendar grid directly by name. The grid is now wrapped
            // in a Border for styling, so searching Plan.Children would miss it.
            _planGrid = PlanCalendarGrid;
        }

        /// <summary>
        /// Snaps _planStartDay back to the Monday of its week so the week view
        /// always shows Monday–Sunday regardless of which day was clicked.
        /// </summary>
        private void AlignPlanStartToMonday()
        {
            int daysSinceMonday = ((int)_planStartDay.DayOfWeek + 6) % 7;
            _planStartDay = _planStartDay.AddDays(-daysSinceMonday).Date;
        }

        /// <summary>
        /// Updates the navigation label to "Week of {Monday} – {Sunday}".
        /// </summary>
        private void UpdatePlanWeekLabel()
        {
            var monday = _planStartDay;
            var sunday = monday.AddDays(6);
            Tb_PlanWeekLabel.Text = $"Week of {monday:dd.MM.yyyy} – {sunday:dd.MM.yyyy}";
        }

        /// <summary>
        /// True if the displayed week has no calendar events and no scheduled tasks.
        /// </summary>
        private bool IsPlanWeekEmpty(DateTime startDay)
        {
            var weekDays = Enumerable.Range(0, 7).Select(i => startDay.AddDays(i).Date);
            bool anyEvents = CalendarEvents.events.Any(e => weekDays.Any(d => e.IsOnDay(d)));
            bool anyTasks = Tasks.nWeek.Values.Any(w => weekDays.Contains(w.Date.Date) && w.Tasks.Count > 0);
            return !anyEvents && !anyTasks;
        }

        private void PlanPrevWeek_Click(object sender, RoutedEventArgs e)
        {
            _planStartDay = _planStartDay.AddDays(-7);
            RefreshPlanView();
        }

        private void PlanNextWeek_Click(object sender, RoutedEventArgs e)
        {
            _planStartDay = _planStartDay.AddDays(7);
            RefreshPlanView();
        }

        private void PlanToday_Click(object sender, RoutedEventArgs e)
        {
            _planStartDay = DateTime.Today;
            RefreshPlanView();
        }

        private void RefreshPlanView()
        {
            if (_planGrid == null) return;
            _planGrid.Children.Clear();
            _planGrid.RowDefinitions.Clear();
            _planGrid.ColumnDefinitions.Clear();

            // Anchor the week to Monday so the label and columns are stable.
            AlignPlanStartToMonday();
            var startDay = _planStartDay;
            UpdatePlanWeekLabel();

            // Empty-state: if the whole week has neither calendar events nor scheduled tasks,
            // show a friendly message instead of an empty calendar grid.
            if (IsPlanWeekEmpty(startDay))
            {
                var emptyText = new TextBlock
                {
                    Text = "No calendar events yet — use the toolbar above to add fixed appointments, work hours, or free time.",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                };
                _planGrid.Children.Add(emptyText);
                return;
            }

            // Outer grid: 1 hour-label column + 7 day columns.
            // Layout matches Google Calendar week-view: time on the left, days as columns.
            _planGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            for (int i = 0; i < 7; i++)
                _planGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

            // === Hour-label column (left rail) ===
            for (int h = 0; h < 24; h++)
            {
                _planGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
                var hourLabel = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(2, 0, 4, 0),
                    Child = new TextBlock
                    {
                        Text = $"{h:D2}:00",
                        FontSize = 10,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top
                    }
                };
                Grid.SetRow(hourLabel, h);
                Grid.SetColumn(hourLabel, 0);
                _planGrid.Children.Add(hourLabel);
            }

            // Spacer row below the hour grid (for tasks footer)
            _planGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var now = DateTime.Now;

            // === Day columns ===
            for (int col = 0; col < 7; col++)
            {
                var day = startDay.AddDays(col);
                var dayEvents = CalendarEvents.events
                    .Where(e => e.IsOnDay(day))
                    .OrderBy(e => e.Start)
                    .ToList();
                var availableHours = CalendarEvents.GetAvailableHoursForDay(day);
                var dayTasks = GetTasksForDay(day);

                // Day-column container: a Grid with 25 rows (24 hours + 1 footer for tasks).
                var dayCol = new Grid();
                for (int h = 0; h < 25; h++)
                    dayCol.RowDefinitions.Add(new RowDefinition { Height = h == 24 ? GridLength.Auto : new GridLength(28) });

                // Background cell grid (so empty hours still show the line)
                for (int h = 0; h < 24; h++)
                {
                    var cell = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5)),
                        BorderThickness = new Thickness(1, 0, 0, 1) // left edge only (right edge is the next column's left edge)
                    };
                    Grid.SetRow(cell, h);
                    dayCol.Children.Add(cell);
                }

                // Vertical day separator on the right edge of the column (except last)
                if (col < 6)
                {
                    var rightEdge = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xAB, 0xAD, 0xB3)),
                        BorderThickness = new Thickness(0, 0, 1, 0)
                    };
                    Grid.SetRow(rightEdge, 0);
                    Grid.SetRowSpan(rightEdge, 24);
                    dayCol.Children.Add(rightEdge);
                }

                // Events: position each into its hour-row, with a tile height
                // proportional to its actual duration. 1h maps to one 28px row.
                foreach (var ev in dayEvents)
                {
                    var startH = Math.Max(0, ev.Start.Hour);
                    var endH = Math.Min(24, ev.End.Hour);
                    var duration = ev.End - ev.Start;
                    var span = Math.Max(1, endH - startH);

                    // Margins trim the tile to fractional hours. A 30-minute event
                    // starting on the hour gets a bottom margin of half a row.
                    const double rowHeight = 28.0;
                    var startOffset = (ev.Start.Minute / 60.0) * rowHeight;
                    var endOffset = (ev.End.Minute / 60.0) * rowHeight;
                    var tileHeight = duration.TotalHours * rowHeight;
                    var topMargin = startOffset;
                    var bottomMargin = Math.Max(0, span * rowHeight - endOffset - tileHeight);

                    var evTile = BuildCalendarEventTile(ev, day, topMargin, bottomMargin, tileHeight);
                    Grid.SetRow(evTile, startH);
                    Grid.SetRowSpan(evTile, span);
                    Grid.SetColumn(evTile, 0);
                    Grid.SetZIndex(evTile, 1);
                    dayCol.Children.Add(evTile);
                }

                // Now-indicator: red 1px line on the current day only.
                if (day.Date == now.Date)
                {
                    var nowLine = BuildNowIndicator(now);
                    Grid.SetRow(nowLine, Math.Max(0, Math.Min(23, now.Hour)));
                    Grid.SetColumn(nowLine, 0);
                    Grid.SetZIndex(nowLine, 10);
                    dayCol.Children.Add(nowLine);
                }

                // Header overlay (day name + date + available badge) sits on top of row 0
                var header = BuildDayHeader(day, dayNames[(int)day.DayOfWeek], availableHours);
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, 0);
                Grid.SetRowSpan(header, 1);
                Grid.SetZIndex(header, 5);
                dayCol.Children.Add(header);

                // Tasks footer (row 24): summary of tasks scheduled for this day
                if (dayTasks.Count > 0)
                {
                    var tasksFooter = BuildDayTasksFooter(dayTasks);
                    Grid.SetRow(tasksFooter, 24);
                    Grid.SetColumn(tasksFooter, 0);
                    dayCol.Children.Add(tasksFooter);
                }

                Grid.SetColumn(dayCol, col + 1);
                Grid.SetRow(dayCol, 0);
                Grid.SetRowSpan(dayCol, 25);
                _planGrid.Children.Add(dayCol);
            }
        }

        /// <summary>
        /// Looks up scheduled tasks for a given day from Tasks.nWeek.
        /// Maps by Week.Date (set in TaskSorter.TryAssignWithSplit) rather than
        /// by integer index, because start-of-week vs. start-from-today are
        /// different anchors.
        /// </summary>
        private List<Task> GetTasksForDay(DateTime day)
        {
            return Tasks.nWeek.Values
                .Where(w => w.Date.Date == day.Date)
                .SelectMany(w => w.Tasks)
                .ToList();
        }

        /// <summary>
        /// Builds a 1-pixel-tall red horizontal line positioned at the current
        /// hour+minute inside today's column. The row is the current hour and
        /// the top margin pushes the line down by the fractional minute offset.
        /// </summary>
        private Border BuildNowIndicator(DateTime now)
        {
            const double rowHeight = 28.0;
            double minuteOffset = (now.Minute / 60.0) * rowHeight;
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xE3, 0x5D, 0x48)), // existing red
                Margin = new Thickness(0, minuteOffset, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private Border BuildCalendarEventTile(CalendarEvent ev, DateTime day, double topMargin, double bottomMargin, double desiredHeight)
        {
            var tile = new Border
            {
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2, topMargin, 2, bottomMargin),
                Padding = new Thickness(4, 2, 4, 2),
                Background = ColorForEventType(ev.Type),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = ev,
                ToolTip = $"Right-click or press ✕ to remove '{ev.Title}'"
            };

            var content = new DockPanel { Margin = new Thickness(0), LastChildFill = true };
            tile.Child = content;

            // Title + time stack fills remaining space
            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock
            {
                Text = ev.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            textStack.Children.Add(new TextBlock
            {
                Text = $"{ev.Start:HH:mm}–{ev.End:HH:mm}",
                FontSize = 10,
                Opacity = 0.75
            });
            content.Children.Add(textStack);

            // Remove button on the right side of the tile
            var removeBtn = new Button
            {
                Content = "✕",
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(4, 0, 0, 0),
                FontSize = 10,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = ev,
                ToolTip = "Remove event",
                VerticalAlignment = VerticalAlignment.Top
            };
            removeBtn.Click += (s, e2) =>
            {
                e2.Handled = true;
                var result = MessageBox.Show(
                    $"Remove '{ev.Title}' ({ev.Start:ddd HH:mm}–{ev.End:HH:mm})?",
                    "Remove calendar event",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) RemoveCalendarEvent(ev);
            };
            DockPanel.SetDock(removeBtn, Dock.Right);
            content.Children.Add(removeBtn);

            // Right-click to remove
            tile.MouseRightButtonDown += (s, e) =>
            {
                e.Handled = true;
                var result = MessageBox.Show(
                    $"Remove '{ev.Title}' ({ev.Start:ddd HH:mm}–{ev.End:HH:mm})?",
                    "Remove calendar event",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) RemoveCalendarEvent(ev);
            };

            return tile;
        }

        private Border BuildDayHeader(DateTime day, string dayName, float availableHours)
        {
            var dock = new DockPanel();
            var dayLabel = new TextBlock
            {
                Text = $"{dayName} {day:dd.MM.}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Margin = new Thickness(2, 0, 0, 0)
            };
            DockPanel.SetDock(dayLabel, Dock.Left);
            dock.Children.Add(dayLabel);

            var availLabel = new TextBlock
            {
                Text = availableHours > 0 ? $"{availableHours:F1}h free" : "full",
                FontSize = 10,
                Foreground = availableHours > 0 ? Brushes.DarkGreen : Brushes.IndianRed,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 0)
            };
            DockPanel.SetDock(availLabel, Dock.Right);
            dock.Children.Add(availLabel);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xAB, 0xAD, 0xB3)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(2, 1, 2, 1),
                Child = dock
            };
        }

        private Border BuildDayTasksFooter(List<Task> scheduledTasks)
        {
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 4, 2, 4) };
            foreach (var task in scheduledTasks.OrderByDescending(t => t.Importance))
            {
                wrap.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(2),
                    Background = ImportanceBrush(task.Importance),
                    Child = new TextBlock
                    {
                        Text = $"{task.Description} ({task.Hours:F1}h)",
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold
                    },
                    ToolTip = $"{task.Description} – {task.Hours:F1}h, P{task.Importance}, deadline {task.Delivery:dd.MM.}"
                });
            }
            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xAB, 0xAD, 0xB3)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(0),
                Child = wrap
            };
        }

        private Brush ImportanceBrush(byte importance) => importance switch
        {
            1 => new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xCD)), // pale yellow
            2 => new SolidColorBrush(Color.FromRgb(0xFC, 0xD8, 0xA4)), // orange
            3 => new SolidColorBrush(Color.FromRgb(0xF5, 0xBC, 0xA9)), // red
            _ => Brushes.LightGray
        };

        private Brush ColorForEventType(CalendarEventType type) => type switch
        {
            CalendarEventType.FixedAppointment => new SolidColorBrush(Color.FromRgb(0xE3, 0x5D, 0x48)), // red
            CalendarEventType.WorkHours => new SolidColorBrush(Color.FromRgb(0xE1, 0xF5, 0xE1)),        // light green
            CalendarEventType.FreeTime => new SolidColorBrush(Color.FromRgb(0x5E, 0x97, 0xD9)),          // blue
            CalendarEventType.Sleep => new SolidColorBrush(Color.FromRgb(0xC9, 0xC9, 0xD9)),             // grey
            _ => Brushes.LightGray
        };

        private void AddCalendarEvent(object sender, RoutedEventArgs e)
        {
            if (Dp_eventDay.SelectedDate == null) return;
            if (string.IsNullOrWhiteSpace(Tb_eventTitle.Text)) return;
            if (Cb_eventStart.SelectedItem == null || Cb_eventEnd.SelectedItem == null) return;

            var day = Dp_eventDay.SelectedDate.Value.Date;
            var startH = Cb_eventStart.SelectedIndex;
            var endH = Cb_eventEnd.SelectedIndex;
            if (endH <= startH) return;

            var ev = new CalendarEvent
            {
                Title = Tb_eventTitle.Text.Trim(),
                Type = (CalendarEventType)Cb_eventType.SelectedIndex,
                Start = day.AddHours(startH),
                End = day.AddHours(endH)
            };

            // Overlap check – v1: just warn, don't block. User might want overlapping work-hours by accident.
            var conflicts = CalendarEvents.events.Where(x => x.IsOnDay(day) && x.ConflictsWith(ev)).ToList();
            if (conflicts.Count > 0)
            {
                Trace.WriteLine($"Warnung: Neuer Event '{ev.Title}' überlappt mit {conflicts.Count} anderen.");
            }

            CalendarEvents.events.Add(ev);
            CalendarEvents.WriteDataToJson();
            Tb_eventTitle.Clear();
            // Re-distribute because adding a calendar event may have changed available slots
            TaskSorter.Distributor();
            RefreshTodoList();
            RefreshWeekPlan();
            RefreshPlanView();
        }

        private void RemoveCalendarEvent(CalendarEvent ev)
        {
            CalendarEvents.events.Remove(ev);
            CalendarEvents.WriteDataToJson();
            // Re-distribute because removing a calendar event frees up slots
            TaskSorter.Distributor();
            RefreshTodoList();
            RefreshWeekPlan();
            RefreshPlanView();
        }

        /// <summary>
        /// Manual re-distribution trigger. User clicks this after adding/removing
        /// CalendarEvents (which changes available hours) or after creating new
        /// tasks (which the auto-distribute on Create() also covers, but this
        /// gives the user control without having to add/remove a task).
        /// </summary>
        private void Redistribute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TaskSorter.Distributor();
                RefreshTodoList();
                RefreshWeekPlan();
                RefreshPlanView();
                Trace.WriteLine("Manual redistribute completed.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Redistribute failed: {ex.Message}");
                MessageBox.Show($"Redistribution failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Timer (focus session) handlers ===

        private void Timer_Start(object sender, RoutedEventArgs e)
        {
            // No tasks = nothing to focus on.
            if (Tasks.tasks.Count == 0)
            {
                MessageBox.Show(
                    "No open tasks yet. Create a task before starting the focus timer.",
                    "Nothing to focus on",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Find the highest-weighted unfinished task that fits in a reasonable focus block.
            // Default to 50 min (Pomodoro-ish) if no task is selected.
            var nextTask = Tasks.tasks.Values
                .Where(t => t.Hours > 0)
                .OrderByDescending(t => t.CalculateWeighting(Tasks.tasks))
                .FirstOrDefault();
            if (nextTask == null)
            {
                MessageBox.Show(
                    "All open tasks are already at 0 hours. Mark something undone or create a new task.",
                    "Nothing to focus on",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Cap focus block to the smaller of 50min or the task's remaining hours
            var blockDuration = TimeSpan.FromMinutes(Math.Min(Settings.focusBlockMinutes, nextTask.Hours * 60));
            if (blockDuration < TimeSpan.FromMinutes(5)) return; // too short, don't bother
            _timer.Start(nextTask, blockDuration);
        }

        private void Timer_Pause(object sender, RoutedEventArgs e)
        {
            if (_timer.IsRunning) _timer.Pause();
            else _timer.Resume();
        }

        private void Timer_Stop(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            // Re-distribute because task hours may have changed
            Calculate();
            RefreshFocusStats();
        }

        private void Timer_SkipBreak(object sender, RoutedEventArgs e)
        {
            _timer.SkipBreak();
        }
    }

    public class WeekPlanViewModel
    {
        public string Day { get; set; } = "";
        public float PlanedHours { get; set; }
        public List<TaskViewModel> Tasks { get; set; } = new();
        public DateTime DayDate { get; set; }
    }

    public class TaskViewModel
    {
        public string Description { get; set; } = "";
        public float Hours { get; set; }
    }
}
