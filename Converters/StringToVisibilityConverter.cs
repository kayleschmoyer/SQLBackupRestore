using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SQLBackupRestore
{
    /// <summary>
    /// Converts a string to Visibility (Visible if not null/empty, Collapsed otherwise).
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public static StringToVisibilityConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
