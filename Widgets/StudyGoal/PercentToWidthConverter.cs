using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MnemoProject.Widgets.StudyGoal
{
    public class PercentToWidthConverter : IValueConverter
    {
        // Singleton instance
        private static PercentToWidthConverter _instance;
        public static PercentToWidthConverter Instance => _instance ??= new PercentToWidthConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int percent)
            {
                // The maximum width is roughly 160px (the width of the container minus padding)
                double maxWidth = 160;
                // Clamp the percentage between 0 and 100
                percent = Math.Clamp(percent, 0, 100);
                // Return the calculated width
                return maxWidth * percent / 100.0;
            }
            
            return 0;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 