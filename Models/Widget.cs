using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using MnemoProject.Services;

namespace MnemoProject.Models
{
    // Enum to identify different widget types
    public enum WidgetType
    {
        WeeklyStudyTime,
        Retention,
        LongestStreak,
        StudyGoal
    }

    // Base class for all widgets
    public partial class Widget : ObservableObject
    {
        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private WidgetType _type;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private int _displayOrder;
        
        [ObservableProperty]
        private int _widthFactor = 1; // Default width factor (1 = standard width, 2 = double width, etc.)
        
        // Returns the actual width based on standard widget width and factor
        public double ActualWidth => 190 * WidthFactor; // 190 is the standard width
        
        // Widget preview control for display in management UI
        // Always create a new instance to avoid Visual Parent conflicts
        public UserControl WidgetPreview => WidgetFactory.CreateWidgetPreview(this);

        public Widget(WidgetType type, string title, string description, bool isEnabled = true, int widthFactor = 1)
        {
            Id = Guid.NewGuid();
            Type = type;
            Title = title;
            Description = description;
            IsEnabled = isEnabled;
            DisplayOrder = 0; // Default display order, will be updated when added to collection
            WidthFactor = widthFactor;
        }
    }
} 