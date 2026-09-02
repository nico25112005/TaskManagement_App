using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskManagement
{
    public class TaskWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double listViewWidth && double.TryParse(parameter?.ToString(), out double taskHours))
            {
                double maxHours = Settings.maxHoursPerDay;

                // Berechnung der Breite basierend auf den Stunden
                if (maxHours > 0)
                {
                    return (taskHours / maxHours) * listViewWidth;
                }
                else
                {
                    // Sicherstellen, dass die maxHours größer als 0 ist
                    return 0;
                }
            }
            return 0; // Rückgabe von 0, falls die Eingabewerte ungültig sind
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
