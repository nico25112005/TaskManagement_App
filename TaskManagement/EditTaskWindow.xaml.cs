using System;
using System.Windows;

namespace TaskManagement
{
    /// <summary>
    /// Modal dialog for editing an existing Task. Returns the modified Task
    /// via the Task property if the user clicks Save, null if they cancel.
    /// </summary>
    public partial class EditTaskWindow : Window
    {
        public Task Task { get; private set; }

        public EditTaskWindow(Task task)
        {
            InitializeComponent();
            Task = task;
            Tb_description.Text = task.Description;
            Tb_hours.Text = task.Hours.ToString("F1");
            Tb_importance.Text = task.Importance.ToString();
            Dp_delivery.SelectedDate = task.Delivery;
            Tb_description.Focus();
            Tb_description.SelectAll();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Tb_description.Text))
            {
                Lbl_error.Text = "Description cannot be empty.";
                return;
            }
            if (!float.TryParse(Tb_hours.Text, out float hours) || hours < 0)
            {
                Lbl_error.Text = "Hours must be a non-negative number.";
                return;
            }
            if (!byte.TryParse(Tb_importance.Text, out byte importance) || importance < 1 || importance > 3)
            {
                Lbl_error.Text = "Importance must be 1, 2 or 3.";
                return;
            }
            if (Dp_delivery.SelectedDate == null)
            {
                Lbl_error.Text = "Pick a delivery date.";
                return;
            }

            Task.Description = Tb_description.Text.Trim();
            Task.Hours = hours;
            Task.Importance = importance;
            Task.Delivery = Dp_delivery.SelectedDate.Value;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
