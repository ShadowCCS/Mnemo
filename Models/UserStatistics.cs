using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Security.Cryptography;

namespace MnemoProject.Models
{
    public class UserStatistics
    {
        // Study time tracking
        public int WeeklyStudyTimeSeconds { get; set; }
        
        // Streak tracking
        public int LongestStreak { get; set; }
        public int CurrentStreak { get; set; }
        
        // Performance tracking
        public double Retention { get; set; }
        
        // Goal tracking
        public int StudyGoalMinutes { get; set; }
        public int CurrentStudyGoalMinutes { get; set; }
        
        // Date tracking for statistics management
        public DateTime LastStudyDate { get; set; } = DateTime.Today;
        public DateTime WeekStartDate { get; set; } = GetStartOfWeek(DateTime.Today);
        
        // Helper method to get start of week (Sunday)
        private static DateTime GetStartOfWeek(DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
        
        // Check if we need to reset weekly stats
        public void CheckWeeklyReset()
        {
            var currentWeekStart = GetStartOfWeek(DateTime.Today);
            if (WeekStartDate < currentWeekStart)
            {
                // Reset weekly statistics
                WeeklyStudyTimeSeconds = 0;
                WeekStartDate = currentWeekStart;
            }
        }
        
        // Check and update streak
        public void UpdateStreak(DateTime studyDate)
        {
            if (LastStudyDate.Date == studyDate.Date)
            {
                // Already recorded for today
                return;
            }
            
            if ((studyDate.Date - LastStudyDate.Date).Days == 1)
            {
                // Consecutive day, increment streak
                CurrentStreak++;
            }
            else if ((studyDate.Date - LastStudyDate.Date).Days > 1)
            {
                // Streak broken
                CurrentStreak = 1;
            }
            
            // Update longest streak if current is longer
            if (CurrentStreak > LongestStreak)
            {
                LongestStreak = CurrentStreak;
            }
            
            LastStudyDate = studyDate.Date;
        }
    }
}