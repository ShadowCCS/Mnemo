using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.StudyGoal
{
    public class StudyGoal : Widget
    {
        // Constructor - use generic default values that will be replaced by localized versions
        public StudyGoal() : base(
            WidgetType.StudyGoal, 
            "Study Goal", 
            "Track your progress toward your daily study goal")
        {
            // Note: We don't need to fetch data here as WidgetService will set the Tag property
        }
        
        // Get the goal progress percentage from Tag property
        public int ProgressPercentage => Tag != null ? Convert.ToInt32(Tag) : 0;
        
        // Load these from the stats service when needed (not cached)
        private void GetGoalStats(out int goalMinutes, out int completedMinutes)
        {
            var stats = StatisticsService.Instance.LoadStatistics();
            goalMinutes = stats.StudyGoalMinutes;
            completedMinutes = stats.CurrentStudyGoalMinutes;
        }
        
        // Show actual progress values in the formatted output
        public string FormattedProgress 
        { 
            get
            {
                GetGoalStats(out int goalMinutes, out int completedMinutes);
                return $"{completedMinutes}/{goalMinutes} min";
            }
        }
    }
} 