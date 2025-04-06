using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MnemoProject.Converters
{
    public class BoolToContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool booleanValue))
                return null;

            string parameters = parameter?.ToString() ?? "|";
            string[] options = parameters.Split('|');

            // If there are two options, return based on the bool value
            if (options.Length >= 2)
            {
                return booleanValue ? options[1] : options[0];
            }

            // Default case
            return booleanValue.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 