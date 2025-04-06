using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.StudyGoal
{
    public class StudyGoal : Widget
    {
        // Properties specific to this widget
        public int GoalMinutes { get; set; }
        public int CompletedMinutes { get; set; }
        
        // Constructor
        public StudyGoal() : base(
            WidgetType.StudyGoal, 
            "Study Goal", 
            "Track your progress toward your daily study goal")
        {
            // Get data from statistics service
            var stats = StatisticsService.Instance.LoadStatistics();
            GoalMinutes = stats.StudyGoalMinutes;
            CompletedMinutes = stats.CurrentStudyGoalMinutes;
        }
        
        // Helper properties for the view
        public int ProgressPercentage => (GoalMinutes > 0) ? (int)(CompletedMinutes * 100 / GoalMinutes) : 0;
        public string FormattedProgress => $"{CompletedMinutes}/{GoalMinutes} min";
    }
} 