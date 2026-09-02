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

        private void RefreshTodoList()
        {
            Lv_Todos.ItemsSource = null;
            Lv_Todos.ItemsSource = Tasks.tasks.Values;
        }

        private void RefreshWeekPlan()
        {
            var weekPlan = Tasks.nWeek.Select(kvp => new WeekPlanViewModel
            {
                Day = $"{kvp.Value.Date:ddd dd.MM.}",
                PlanedHours = kvp.Value.PlanedHours,
                Tasks = kvp.Value.Tasks.Select(task => new TaskViewModel { Description = task.Description }).ToList()
            }).ToList();

            Lv_WeekPlan.ItemsSource = weekPlan;
        }

        private void WeekPlan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is TaskViewModel task)
            {
                _draggedTask = task;
                DragDrop.DoDragDrop(item, task, DragDropEffects.Move);
            }
        }

        private void TaskList_Drop(object sender, DragEventArgs e)
        {
            if (_draggedTask == null || !(sender is ItemsControl targetList)) return;

            var targetWeek = targetList.DataContext as WeekPlanViewModel;
            if (targetWeek != null)
            {
                Trace.WriteLine($"Verschiebe Aufgabe '{_draggedTask.Description}' zu {targetWeek.Day}");
                RemoveTaskFromCurrentWeek(_draggedTask);
                targetWeek.Tasks.Add(_draggedTask);
                UpdateWeekPlanAfterTaskMove();
            }

            _draggedTask = null;
        }

        private void RemoveTaskFromCurrentWeek(TaskViewModel task)
        {
            foreach (var week in Tasks.nWeek.Values)
            {
                var taskToRemove = week.Tasks.FirstOrDefault(t => t.Description == task.Description);
                if (taskToRemove != null)
                {
                    week.Tasks.Remove(taskToRemove);
                    week.PlanedHours -= taskToRemove.Hours;
                    Trace.WriteLine($"Aufgabe '{task.Description}' aus ursprünglichem Tag entfernt.");
                    break;
                }
            }
        }

        private void UpdateWeekPlanAfterTaskMove()
        {
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

            // 7 day columns are already declared in XAML.
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek); // Sunday
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

            for (int col = 0; col < 7; col++)
            {
                var day = startOfWeek.AddDays(col);
                var dayEvents = CalendarEvents.events
                    .Where(e => e.IsOnDay(day))
                    .OrderBy(e => e.Start)
                    .ToList();
                var availableHours = CalendarEvents.GetAvailableHoursForDay(day);

                var dayPanel = BuildDayPanel(day, dayNames[col], dayEvents, availableHours);
                Grid.SetColumn(dayPanel, col);
                _planGrid.Children.Add(dayPanel);
            }
        }

        private Border BuildDayPanel(DateTime day, string dayName, List<CalendarEvent> events, float availableHours)
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
                    Child = new StackPanel
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
                // Right-click to remove (simple v1 affordance)
                evBorder.MouseRightButtonUp += (s, e) => RemoveCalendarEvent(ev);
                evBorder.ToolTip = "Right-click to remove";
                stack.Children.Add(evBorder);
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
            RefreshPlanView();
        }

        private void RemoveCalendarEvent(CalendarEvent ev)
        {
            CalendarEvents.events.Remove(ev);
            CalendarEvents.WriteDataToJson();
            RefreshPlanView();
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
        public string Day { get; set; }
        public float PlanedHours { get; set; }
        public List<TaskViewModel> Tasks { get; set; }
    }

    public class TaskViewModel
    {
        public string Description { get; set; }
    }
}
