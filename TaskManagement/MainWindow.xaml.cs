using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace TaskManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StartUp();
            SetColors();
        }
        private void Create(object sender, RoutedEventArgs e)
        {

            if (Tb_description.Text != "" || Tb_hours.Text != "" || Tb_importancy.Text != "" || Dp_delivery_date.SelectedDate != null) // §funktioniert nicht :(
            {
                Data.tasks.Add(Task.GenerateID(), new Task(float.Parse(Tb_hours.Text), Tb_description.Text, Dp_delivery_date.SelectedDate.GetValueOrDefault(), byte.Parse(Tb_importancy.Text), false));
                Lv_Todos.ItemsSource = null;
                Lv_Todos.ItemsSource = Data.tasks.Values;
                Data.WriteDataToJson<Dictionary<string, Task>>("Todos", Data.tasks);

            }

            Calculate();
        }

        private void StartUp()
        {
            Data.ReadDataOfJson<Dictionary<string, Task>>("Todos", out Data.tasks);
            Data.GenerateTasks(5, 1, 5, 1, 3, false);
            TaskSorter.Distributor();
            if (Data.tasks != null)
            {
                Lv_Todos.ItemsSource = Data.tasks.Values;
            }
            if (Data.nWeek != null)
            {
                Lv_MenuTodos.ItemsSource = Data.nWeek[0].Tasks;
            }
        }

        private void Calculate()
        {

            Data.nWeek.Clear();
            TaskSorter.Distributor();
            if (Data.tasks != null)
            {
                Lv_Todos.ItemsSource = Data.tasks.Values;
            }
            if (Data.notDistributableTasks != null)
            {
                Lv_MenuTodos.ItemsSource = Data.nWeek[0].Tasks;
            }
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

        private Brush? Colorpalet(byte index)
        {
            List<string> hexColors = new List<string>()
            {
                "#5E97D9",
                "#e35d48",
                "#FFC941",
                "#509C6E"

            };

            var bc = new BrushConverter();
            if (hexColors[index] != null)
            {
                return bc.ConvertFrom(hexColors[index]) as Brush;
            }
            else
            {
                return Brushes.White;
            }
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
        }

        private void Settings_page(object sender, RoutedEventArgs e)
        {
            ChangePage(Pages.Settings);
        }
    }
}