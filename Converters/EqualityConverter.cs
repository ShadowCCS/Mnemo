using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MnemoProject.Converters
{
    public class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
                return parameter?.ToString();
            
            return null;
        }
    }
} 