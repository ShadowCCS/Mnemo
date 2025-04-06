using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.LongestStreak
{
    public class LongestStreak : Widget
    {
        public int LongestStreakDays { get; set; }
        
        public LongestStreak() : base(
            WidgetType.LongestStreak, 
            "Longest Streak", 
            "Track your longest consecutive study streak")
        {
            // Get data from statistics service
            var stats = StatisticsService.Instance.LoadStatistics();
            LongestStreakDays = stats.LongestStreak;
        }
        
        public string FormattedLongestStreak => $"{LongestStreakDays} days";
    }
} 