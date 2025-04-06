using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MnemoProject.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MnemoProject.Services
{
    /// <summary>
    /// Factory for creating widget controls based on widget type
    /// </summary>
    public static class WidgetFactory
    {
        private static readonly Dictionary<WidgetType, Type> _widgetViewTypes = new Dictionary<WidgetType, Type>();
        private static readonly Dictionary<WidgetType, Type> _widgetTypes = new Dictionary<WidgetType, Type>();
        
        static WidgetFactory()
        {
            // Scan and register all widget types during initialization
            ScanAndRegisterWidgetTypes();
        }
        
        private static void ScanAndRegisterWidgetTypes()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                // Find all types in the Widgets namespace
                var widgetTypes = assembly.GetTypes()
                    .Where(t => t.Namespace != null && 
                                t.Namespace.StartsWith("MnemoProject.Widgets.") && 
                                t.IsClass && 
                                !t.IsAbstract &&
                                t.IsSubclassOf(typeof(Widget)));
                
                // Find all widget view types
                var widgetViewTypes = assembly.GetTypes()
                    .Where(t => t.Namespace != null && 
                                t.Namespace.StartsWith("MnemoProject.Widgets.") && 
                                t.Name.EndsWith("View") &&
                                t.IsSubclassOf(typeof(UserControl)));
                
                foreach (var widgetType in widgetTypes)
                {
                    // Create an instance to get its type
                    if (Activator.CreateInstance(widgetType) is Widget widget)
                    {
                        _widgetTypes[widget.Type] = widgetType;
                        
                        // Look for corresponding view type
                        string viewTypeName = $"{widgetType.Name}View";
                        var viewType = widgetViewTypes.FirstOrDefault(t => t.Name == viewTypeName);
                        if (viewType != null)
                        {
                            _widgetViewTypes[widget.Type] = viewType;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error scanning widget types: {ex.Message}");
            }
        }
        
        public static UserControl CreateWidgetControl(Widget widget)
        {
            if (widget == null)
                return null;
            
            if (_widgetViewTypes.TryGetValue(widget.Type, out var viewType))
            {
                var control = (UserControl)Activator.CreateInstance(viewType);
                control.DataContext = widget;
                return control;
            }
            
            // Fallback to specific implementation if not found through reflection
            return widget.Type switch
            {
                WidgetType.WeeklyStudyTime => new MnemoProject.Widgets.WeeklyStudyTime.WeeklyStudyTimeView(),
                WidgetType.Retention => new MnemoProject.Widgets.Retention.RetentionView(),
                WidgetType.StudyGoal => new MnemoProject.Widgets.StudyGoal.StudyGoalView(),
                WidgetType.LongestStreak => new MnemoProject.Widgets.LongestStreak.LongestStreakView(),
                _ => throw new ArgumentException($"Unsupported widget type: {widget.Type}")
            };
        }
        
        public static Widget CreateWidget(WidgetType type)
        {
            if (_widgetTypes.TryGetValue(type, out var widgetType))
            {
                return (Widget)Activator.CreateInstance(widgetType);
            }
            
            // Fallback for types without specific implementation
            return new Widget(type, type.ToString(), $"Widget for {type}");
        }

        public static UserControl CreateNewWidgetButton()
        {
            return new Views.Controls.Widgets.NewWidgetButton();
        }
        
        // Get a list of all available widget types with sample widgets for preview
        public static List<Widget> GetAllWidgetSamples()
        {
            var samples = new List<Widget>();
            
            foreach (var type in Enum.GetValues(typeof(WidgetType)).Cast<WidgetType>())
            {
                if (_widgetTypes.TryGetValue(type, out var widgetType))
                {
                    samples.Add((Widget)Activator.CreateInstance(widgetType));
                }
                else
                {
                    samples.Add(new Widget(type, type.ToString(), $"Widget for {type}"));
                }
            }
            
            return samples;
        }
        
        // Get a widget thumbnail/preview for display in the ManageWidgets overlay
        public static UserControl CreateWidgetPreview(Widget widget)
        {
            if (widget == null)
                return null;
                
            try
            {
                // Create a new control instance (never reuse) for preview
                UserControl control;
                
                if (_widgetViewTypes.TryGetValue(widget.Type, out var viewType))
                {
                    control = (UserControl)Activator.CreateInstance(viewType);
                }
                else
                {
                    // Fallback to the specific type implementation
                    control = widget.Type switch
                    {
                        WidgetType.WeeklyStudyTime => new MnemoProject.Widgets.WeeklyStudyTime.WeeklyStudyTimeView(),
                        WidgetType.Retention => new MnemoProject.Widgets.Retention.RetentionView(),
                        WidgetType.StudyGoal => new MnemoProject.Widgets.StudyGoal.StudyGoalView(),
                        WidgetType.LongestStreak => new MnemoProject.Widgets.LongestStreak.LongestStreakView(),
                        _ => throw new ArgumentException($"Unsupported widget type: {widget.Type}")
                    };
                }
                
                // Set the DataContext
                control.DataContext = widget;
                
                // Calculate preview width based on widget's width factor
                double previewWidth = 120 * widget.WidthFactor;
                
                // Use a Grid as the scaling container to properly position the scaled content
                var container = new Grid
                {
                    Width = previewWidth,
                    Height = 80
                };
                
                // Set the original size of the control to match its normal dimensions
                control.Width = widget.ActualWidth;
                control.Height = 100;
                
                // Apply scaling transform to the control
                double scaleFactor = 0.63;
                control.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
                control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                
                // Center the control in the container
                control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                control.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                
                // Add the control to the container
                container.Children.Add(control);
                
                // Create a wrapper user control to return
                var wrapper = new UserControl
                {
                    Content = container
                };
                
                return wrapper;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating widget preview: {ex.Message}");
                return null;
            }
        }

        // Public method to force rescanning of widget types
        public static void RescanWidgetTypes()
        {
            // Clear existing registrations
            _widgetViewTypes.Clear();
            _widgetTypes.Clear();
            
            // Scan and register again
            ScanAndRegisterWidgetTypes();
        }
    }
} 