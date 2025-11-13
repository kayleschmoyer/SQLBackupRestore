using System;
using System.Globalization;
using System.Windows.Data;

namespace SQLBackupRestore
{
    /// <summary>
    /// Converts enum values to boolean for radio button binding.
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public static EnumToBooleanConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
                return parameter;

            return Binding.DoNothing;
        }
    }
}
