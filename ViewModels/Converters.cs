using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using Avalonia.Data;
using System.Collections.Generic;
using Avalonia;
using System.Collections;

namespace MnemoProject.ViewModels
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public static readonly BooleanToBrushConverter Instance = new BooleanToBrushConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Parameter should be in format "TrueBrush,FalseBrush"
                if (parameter is string colors)
                {
                    var parts = colors.Split(',');
                    if (parts.Length >= 2)
                    {
                        var colorStr = boolValue ? parts[0] : parts[1];
                        return new SolidColorBrush(Color.Parse(colorStr));
                    }
                }
                
                // Default colors if no parameters provided
                return new SolidColorBrush(boolValue ? Colors.Green : Colors.Red);
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }

    public class BooleanToStringConverter : IValueConverter
    {
        public static readonly BooleanToStringConverter Instance = new BooleanToStringConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string stringParams)
            {
                string[] options = stringParams.Split(',');
                if (options.Length == 2)
                {
                    return boolValue ? options[0] : options[1];
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToBooleanConverter : IValueConverter
    {
        public static readonly EnumToBooleanConverter Instance = new EnumToBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
                
            // For string parameters (from XAML)
            if (parameter is string parameterString)
            {
                // Try parsing as an integer
                if (int.TryParse(parameterString, out int intValue))
                {
                    return value.Equals(intValue);
                }
                
                // Otherwise, try as an enum value
                return value.ToString().Equals(parameterString, StringComparison.OrdinalIgnoreCase);
            }
            
            // For direct object comparison
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                if (parameter is string parameterString)
                {
                    // Try converting string to integer
                    if (int.TryParse(parameterString, out int intValue) && typeof(int).IsAssignableFrom(targetType))
                    {
                        return intValue;
                    }
                    
                    // Try converting string to enum
                    if (targetType.IsEnum)
                    {
                        return Enum.Parse(targetType, parameterString, true);
                    }
                }
                
                return parameter;
            }
            
            return AvaloniaProperty.UnsetValue;
        }
    }
    
    public class NullCheckConverter : IValueConverter
    {
        public static readonly NullCheckConverter Instance = new NullCheckConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // For collections, check if they exist and have items
            if (value is ICollection collection)
            {
                return collection != null && collection.Count > 0;
            }
            
            // For other objects, just check if not null
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }

    public class StringEqualityToBrushConverter : IValueConverter
    {
        public static readonly StringEqualityToBrushConverter Instance = new StringEqualityToBrushConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If either value is null, return default brush
            if (value == null || parameter == null)
                return new SolidColorBrush(Colors.Transparent);
                
            // Get the string value of the button content (parameter comes from CommandParameter)
            string buttonOption = parameter.ToString();
            
            // Get the currently selected option from the view model (UserAnswer property)
            string selectedOption = value.ToString();
            
            // Get the colors for selected/unselected states
            string colorParts = "#2880b1,#3a3a3a"; // Default colors
            
            string[] colors = colorParts.Split(',');
            
            // If this button's option matches the selected option, use selected color
            if (string.Equals(buttonOption, selectedOption, StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.Parse(colors[0]));
            }
            
            // Otherwise use default color
            return new SolidColorBrush(Color.Parse(colors[1]));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IndexVisibilityConverter : IValueConverter
    {
        public static readonly IndexVisibilityConverter Instance = new IndexVisibilityConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return false (not visible) for null collections
            if (value == null)
                return false;
                
            // If the value is a count, check if it's greater than the index
            if (value is int count && parameter is string indexStr && int.TryParse(indexStr, out int index))
            {
                return count > index;
            }
            
            // Handle collections by checking their count
            if (value is ICollection collection && parameter is string indexParam && int.TryParse(indexParam, out int indexValue))
            {
                return collection.Count > indexValue;
            }
            
            // If we get here, we can't determine visibility
            return false;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }
    
    public class IntegerGreaterThanConverter : IValueConverter
    {
        public static readonly IntegerGreaterThanConverter Instance = new IntegerGreaterThanConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return false for null values
            if (value == null)
                return false;
            
            // Parse the parameter to an integer
            if (!(parameter is string paramStr && int.TryParse(paramStr, out int threshold)))
                return false;
            
            // If the value is an integer, compare directly
            if (value is int intValue)
                return intValue > threshold;
            
            // Try to parse the value as an integer
            if (value is string valueStr && int.TryParse(valueStr, out int parsedValue))
                return parsedValue > threshold;
            
            // If it's a collection, check count
            if (value is ICollection collection)
                return collection.Count > threshold;
            
            // Default case
            return false;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }
} 