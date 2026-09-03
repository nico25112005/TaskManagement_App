using System;
using System.Windows;
using System.Windows.Input;

namespace TaskManagement
{
    /// <summary>
    /// Minimal overlay for capturing a new task fast (Ctrl+Shift+N from anywhere).
    /// Enter commits with sensible defaults (1h, importance 2, delivery = today + 7d).
    /// Esc cancels.
    /// </summary>
    public partial class QuickCaptureWindow : Window
    {
        public string CapturedDescription { get; private set; } = "";

        public QuickCaptureWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => { Tb_input.Focus(); };
        }

        private void Tb_input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Commit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        private void Commit()
        {
            if (string.IsNullOrWhiteSpace(Tb_input.Text)) return;
            CapturedDescription = Tb_input.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}
