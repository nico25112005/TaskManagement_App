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

        public MainWindow()
        {
            InitializeComponent();
            StartUp();
            SetColors();
        }

        private void StartUp()
        {
            try
            {
                Tasks.ReadDataFromJson<Dictionary<string, Task>>("todos", out Tasks.tasks);

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
                Day = $"{DateTime.Now.AddDays(kvp.Key).ToString("M")}",
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

        private void Settings_page(object sender, RoutedEventArgs e)
        {
            ChangePage(Pages.Settings);
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
