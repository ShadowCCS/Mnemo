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
            var now = DateTime.Now;
            var lastStudy = LastStudyDate;

            // If it's been more than 7 days since the last study, reset weekly stats
            if ((now - lastStudy).TotalDays >= 7)
            {
                WeeklyStudyTimeSeconds = 0;
                CurrentStudyGoalMinutes = 0;
            }
        }
        
        // Check and update streak
        public void UpdateStreak(DateTime studyDate)
        {
            var lastStudy = LastStudyDate;
            var timeSinceLastStudy = studyDate - lastStudy;

            if (timeSinceLastStudy.TotalDays <= 1)
            {
                // Same day or next day, increment streak
                CurrentStreak++;
                if (CurrentStreak > LongestStreak)
                {
                    LongestStreak = CurrentStreak;
                }
            }
            else if (timeSinceLastStudy.TotalDays > 1)
            {
                // More than one day gap, reset streak
                CurrentStreak = 1;
            }

            LastStudyDate = studyDate;
        }
    }
}