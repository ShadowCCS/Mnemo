using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.WeeklyStudyTime
{
    public class WeeklyStudyTime : Widget
    {
        // Time spent studying in the current week (in minutes)
        public int WeeklyStudyTimeMinutes { get; set; }
        
        // Constructor
        public WeeklyStudyTime() : base(
            WidgetType.WeeklyStudyTime, 
            "Weekly Study Time", 
            "Shows you how much you have studied the last week")
        {
            // Get data from statistics service
            var stats = StatisticsService.Instance.LoadStatistics();
            WeeklyStudyTimeMinutes = stats.WeeklyStudyTimeSeconds / 60; // Convert seconds to minutes
        }
        
        // Format the time for display
        public string FormattedStudyTime
        {
            get
            {
                int hours = WeeklyStudyTimeMinutes / 60;
                int minutes = WeeklyStudyTimeMinutes % 60;
                
                if (hours > 0)
                {
                    return $"{hours}h {minutes}m";
                }
                else
                {
                    return $"{minutes}m";
                }
            }
        }
    }
} 