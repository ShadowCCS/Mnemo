using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.LongestStreak
{
    public class LongestStreak : Widget
    {
        public LongestStreak() : base(
            WidgetType.LongestStreak, 
            "Longest Streak", 
            "Track your longest consecutive study streak")
        {
            // Note: We don't need to fetch data here as WidgetService will set the Tag property
        }
        
        // Use the Tag property to get the streak days
        public int LongestStreakDays => Tag != null ? Convert.ToInt32(Tag) : 0;
        
        public string FormattedLongestStreak => $"{LongestStreakDays} days";
    }
} 