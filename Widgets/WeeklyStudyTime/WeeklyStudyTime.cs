using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject.Widgets.WeeklyStudyTime
{
    public class WeeklyStudyTime : Widget
    {
        // Constructor - use generic default values that will be replaced by localized versions
        public WeeklyStudyTime() : base(
            WidgetType.WeeklyStudyTime, 
            "Weekly Study Time", 
            "Shows weekly study time")
        {
            // Note: We don't need to fetch data here as WidgetService will set the Tag property
        }
        
        // Get study time in hours from Tag property
        public double StudyTimeHours => Tag != null ? Convert.ToDouble(Tag) : 0;
        
        // Format the time for display
        public string FormattedStudyTime
        {
            get
            {
                double hours = StudyTimeHours;
                int wholeHours = (int)Math.Floor(hours);
                int minutes = (int)Math.Round((hours - wholeHours) * 60);
                
                if (wholeHours > 0)
                {
                    return $"{wholeHours}h {minutes}m";
                }
                else
                {
                    return $"{minutes}m";
                }
            }
        }
    }
} 