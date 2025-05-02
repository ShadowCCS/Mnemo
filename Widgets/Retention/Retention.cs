using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.Retention
{
    public class Retention : Widget
    {
        // Constructor - use generic default values that will be replaced by localized versions
        public Retention() : base(
            WidgetType.Retention, 
            "Retention", 
            "Shows retention rate")
        {
            // Note: We don't need to fetch data here as WidgetService will set the Tag property
        }
        
        // Use the Tag property to get the current retention percentage
        public double RetentionPercentage => Tag != null ? Convert.ToDouble(Tag) : 0;
        
        // Format the percentage for display
        public string FormattedRetention => $"{RetentionPercentage:F1}%";
    }
} 