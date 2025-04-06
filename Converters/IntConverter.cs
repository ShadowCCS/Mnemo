using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MnemoProject.Converters
{
    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue;
            
            if (value is string strValue && int.TryParse(strValue, out int result))
                return result;
            
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue;
                
            return 0;
        }
    }
} 