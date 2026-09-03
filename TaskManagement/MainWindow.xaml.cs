using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;

namespace TaskManagement
{
    public partial class MainWindow : Window
    {
        private TaskViewModel _draggedTask;
        private TimerViewModel _timer = new();

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
            StartUp();
            SetColors();
            InitPlanView();
        }

        private void StartUp()
        {
            try
            {
                Tasks.ReadDataFromJson<Dictionary<string, Task>>("todos", out Tasks.tasks);
                CalendarEvents.ReadDataFromJson();

                //Tasks.GenerateTasks(6, 1, 5, 1, 3, false);
                TaskSorter.Distributor();
                RefreshTodoList();
                RefreshWeekPlan();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Fehler bei StartUp: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Fehler beim Erstellen der Aufgabe: {ex.Message}");
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
            // Refresh the Todo (create-new-task) page's full list – unchanged.
            Lv_Todos.ItemsSource = null;
            Lv_Todos.ItemsSource = Tasks.tasks.Values;

            // Refresh the Home page's Today/Upcoming buckets via the view-model.
            _homeTodo.Refresh();
            Lv_TodayTodos.ItemsSource = _homeTodo.Today;
            Lv_UpcomingTodos.ItemsSource = _homeTodo.Upcoming;
            TodoLoadLabel.Content = _homeTodo.TodayLoadLabel;
            TodoLoadLabel.Foreground = _homeTodo.IsOverloaded ? Brushes.IndianRed : Brushes.Gray;
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
        }

        private void DayTaskContainer_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(TaskViewModel)) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
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

            var result = MessageBox.Show(
                $"Delete task '{task.Description}'?",
                "Delete task",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            // Remove from the dictionary by matching description (matches our todo-key pattern)
            var key = Tasks.tasks.FirstOrDefault(kvp => kvp.Value == task).Key;
            if (key == null)
            {
                // Fallback: search by reference equality via fields
                key = Tasks.tasks.FirstOrDefault(kvp => ReferenceEquals(kvp.Value, task)).Key;
            }
            if (key == null)
            {
                Trace.WriteLine($"DeleteTodo_Click: task reference not found in dictionary.");
                return;
            }

            Tasks.tasks.Remove(key);
            Tasks.WriteDataToJson<Dictionary<string, Task>>("todos", Tasks.tasks);
            Calculate(); // re-distribute + refresh all views
            Trace.WriteLine($"Deleted task '{task.Description}'.");
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
                { Pages.Settings, Settings }
            };

            foreach (var page in pageMap)
            {
                page.Value.Visibility = page.Key == pages ? Visibility.Visible : Visibility.Collapsed;
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

            // Find the inner Grid of the Plan tab and keep a reference.
            // (The XAML has exactly one child Grid in Plan's Row 1.)
            _planGrid = Plan.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 1);
        }

        private void RefreshPlanView()
        {
            if (_planGrid == null) return;
            _planGrid.Children.Clear();
            _planGrid.RowDefinitions.Clear();

            // Start from today, run 7 days. This matches the TaskSorter horizon
            // (Tasks.nWeek is keyed by DateTime.Today.AddDays(N)), so we can map
            // tasks to days directly by their Date property.
            var startDay = DateTime.Today;
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

            for (int col = 0; col < 7; col++)
            {
                var day = startDay.AddDays(col);
                var dayEvents = CalendarEvents.events
                    .Where(e => e.IsOnDay(day))
                    .OrderBy(e => e.Start)
                    .ToList();
                var availableHours = CalendarEvents.GetAvailableHoursForDay(day);
                var dayTasks = GetTasksForDay(day);

                var dayPanel = BuildDayPanel(day, dayNames[(int)day.DayOfWeek], dayEvents, availableHours, dayTasks);
                Grid.SetColumn(dayPanel, col);
                _planGrid.Children.Add(dayPanel);
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

        private Border BuildDayPanel(DateTime day, string dayName, List<CalendarEvent> events, float availableHours, List<Task> scheduledTasks)
        {
            var stack = new StackPanel { Margin = new Thickness(4) };

            // Header: day name + date + available-hours badge
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var dayLabel = new Label
            {
                Content = $"{dayName} {day:dd.MM}",
                FontWeight = FontWeights.Bold,
                FontSize = 13
            };
            DockPanel.SetDock(dayLabel, Dock.Left);
            header.Children.Add(dayLabel);

            var availLabel = new Label
            {
                Content = availableHours > 0 ? $"{availableHours:F1}h free" : "full",
                HorizontalAlignment = HorizontalAlignment.Right,
                FontSize = 11,
                Foreground = availableHours > 0 ? Brushes.DarkGreen : Brushes.IndianRed
            };
            DockPanel.SetDock(availLabel, Dock.Right);
            header.Children.Add(availLabel);
            stack.Children.Add(header);

            // Events for the day, color-coded by type
            foreach (var ev in events)
            {
                var evBorder = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 3, 6, 3),
                    Margin = new Thickness(0, 2, 0, 2),
                    Background = ColorForEventType(ev.Type),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = $"Right-click or press ✕ to remove '{ev.Title}'",
                    Tag = ev,
                    Child = new DockPanel
                    {
                        Children =
                        {
                            new Label
                            {
                                Content = ev.Title,
                                FontWeight = FontWeights.SemiBold,
                                FontSize = 11,
                                Padding = new Thickness(0)
                            },
                            new Label
                            {
                                Content = $"{ev.Start:HH:mm}–{ev.End:HH:mm}",
                                FontSize = 10,
                                Opacity = 0.7,
                                Padding = new Thickness(0)
                            }
                        }
                    }
                };

                // Visible Remove button (✕) inside the event tile. The event reference
                // travels via Border.Tag, so the click handler can pull it back out.
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
                    ToolTip = "Remove event"
                };
                removeBtn.Click += (s, e2) =>
                {
                    e2.Handled = true;
                    var result = MessageBox.Show(
                        $"Remove '{ev.Title}' ({ev.Start:ddd HH:mm}–{ev.End:HH:mm})?",
                        "Remove calendar event",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        RemoveCalendarEvent(ev);
                    }
                };
                DockPanel.SetDock(removeBtn, Dock.Right);
                ((DockPanel)evBorder.Child).Children.Insert(0, removeBtn);

                // Right-click to remove. Use MouseRightButtonDown (not Up) because
                // Up can be swallowed by ancestor context-menus or scroll-behaviour;
                // Down + e.Handled = true guarantees the event is consumed.
                evBorder.MouseRightButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    var result = MessageBox.Show(
                        $"Remove '{ev.Title}' ({ev.Start:ddd HH:mm}–{ev.End:HH:mm})?",
                        "Remove calendar event",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        RemoveCalendarEvent(ev);
                    }
                };

                stack.Children.Add(evBorder);
            }

            // === Scheduled tasks section ===
            // Visual separator between events (immovable) and tasks (movable).
            // This makes the planner read top-to-bottom: what's locked in first,
            // then what you're working through.
            if (scheduledTasks.Count > 0)
            {
                var separator = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                    Margin = new Thickness(0, 6, 0, 4)
                };
                stack.Children.Add(separator);

                var tasksHeader = new Label
                {
                    Content = "Tasks",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Gray,
                    Padding = new Thickness(0, 0, 0, 2)
                };
                stack.Children.Add(tasksHeader);

                foreach (var task in scheduledTasks.OrderByDescending(t => t.Importance))
                {
                    var hoursLabel = new Label
                    {
                        Content = $"{task.Hours:F1}h",
                        FontSize = 10,
                        Opacity = 0.7,
                        Padding = new Thickness(0, 0, 0, 0)
                    };
                    var descLabel = new Label
                    {
                        Content = task.Description,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Padding = new Thickness(0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };

                    var innerDock = new DockPanel();
                    DockPanel.SetDock(hoursLabel, Dock.Right);
                    innerDock.Children.Add(hoursLabel);
                    innerDock.Children.Add(descLabel);

                    var taskBorder = new Border
                    {
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 3, 6, 3),
                        Margin = new Thickness(0, 2, 0, 2),
                        Background = ImportanceBrush(task.Importance),
                        ToolTip = $"{task.Description} – {task.Hours:F1}h, importance {task.Importance}, deadline {task.Delivery:dd.MM.}",
                        Child = innerDock
                    };

                    stack.Children.Add(taskBorder);
                }
            }

            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xAB, 0xAD, 0xB3)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(2),
                Padding = new Thickness(4),
                Child = stack
            };
        }

        /// <summary>
        /// Color coding for task chips based on importance (1=low, 3=high).
        /// Cool to warm: pale yellow -> orange -> red.
        /// </summary>
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
            // Find the highest-weighted unfinished task that fits in a reasonable focus block.
            // Default to 50 min (Pomodoro-ish) if no task is selected.
            var nextTask = Tasks.tasks.Values
                .Where(t => t.Hours > 0)
                .OrderByDescending(t => t.CalculateWeighting(Tasks.tasks))
                .FirstOrDefault();
            if (nextTask == null) return;

            // Cap focus block to the smaller of 50min or the task's remaining hours
            var blockDuration = TimeSpan.FromMinutes(Math.Min(50, nextTask.Hours * 60));
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
