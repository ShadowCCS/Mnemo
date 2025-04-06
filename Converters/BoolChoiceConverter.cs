using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MnemoProject.Converters
{
    public class BoolChoiceConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3 || values[0] is not bool isFlipped)
                return "No content available";

            // Return the second value (CurrentQuestion) if not flipped, 
            // or the third value (CurrentAnswer) if flipped
            return isFlipped ? values[2]?.ToString() : values[1]?.ToString();
        }
    }
} 