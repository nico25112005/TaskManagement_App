using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TaskManagement
{
    /// <summary>
    /// Converts a float hours-value into a pixel width for the week-plan task bar.
    /// Uses the ConverterParameter as the scale (hours-to-pixels factor).
    /// Example: Hours=5, parameter=30 -> width = 150px.
    /// Min 30px so empty/very-short tasks are still visible.
    /// </summary>
    public class TaskWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float hours && double.TryParse(parameter?.ToString(), out double scale))
            {
                double width = hours * scale;
                return Math.Max(30, Math.Min(400, width));
            }
            return 30.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
