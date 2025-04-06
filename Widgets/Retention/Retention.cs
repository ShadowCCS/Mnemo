using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.Retention
{
    public class Retention : Widget
    {
        // Retention percentage
        public double RetentionPercentage { get; set; }
        
        // Constructor
        public Retention() : base(
            WidgetType.Retention, 
            "Retention", 
            "Shows how well you remember your flashcards")
        {
            // Get data from statistics service
            var stats = StatisticsService.Instance.LoadStatistics();
            RetentionPercentage = stats.Retention;
        }
        
        // Format the percentage for display
        public string FormattedRetention => $"{RetentionPercentage:F1}%";
    }
} 