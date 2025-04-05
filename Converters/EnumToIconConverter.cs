using Avalonia.Data;
using Avalonia.Data.Converters;
using MnemoProject.Services;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MnemoProject.Converters
{
    public class EnumToIconConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Count < 6 || values[0] == null)
                return null;

            if (values[0] is NotificationType type)
            {
                switch (type)
                {
                    case NotificationType.Info:
                        return values[1];
                    case NotificationType.Success:
                        return values[2];
                    case NotificationType.Warning:
                        return values[3];
                    case NotificationType.Error:
                        return values[4];
                    case NotificationType.AIGeneration:
                        return values[5];
                    default:
                        return values[1]; // Default to info icon
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 