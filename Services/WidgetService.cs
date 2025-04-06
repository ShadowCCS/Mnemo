using MnemoProject.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MnemoProject.Services
{
    public class WidgetService
    {
        private static WidgetService _instance;
        public static WidgetService Instance => _instance ??= new WidgetService();

        private readonly ObservableCollection<Widget> _widgets = new();
        public ObservableCollection<Widget> Widgets => _widgets;

        // Separate collections for enabled and disabled widgets
        private readonly ObservableCollection<Widget> _enabledWidgets = new();
        public ObservableCollection<Widget> EnabledWidgets => _enabledWidgets;

        private readonly ObservableCollection<Widget> _disabledWidgets = new();
        public ObservableCollection<Widget> DisabledWidgets => _disabledWidgets;

        // Maximum number of widgets allowed
        private const int MaxWidgetsAllowed = 8;
        public int MaxWidgets => MaxWidgetsAllowed;
        
        // Check if more widgets can be added
        public bool CanAddMoreWidgets => _enabledWidgets.Count < MaxWidgetsAllowed;

        private bool _isInitialized = false;
        private bool _isLoading = false;

        // Public property to check if widgets are already initialized
        public bool IsInitialized => _isInitialized;

        // Event that fires when widgets finish loading
        public event EventHandler? WidgetsLoadCompleted;

        private WidgetService()
        {
            InitializeWidgets();
        }

        // Load settings asynchronously when application starts
        public async Task LoadSettingsAsync()
        {
            if (_isLoading) return;
            
            _isLoading = true;
            
            try
            {
                await AppSettings.LoadAsync();
                LoadWidgetConfigFromSettings();
                
                // Set initialized flag
                _isInitialized = true;
                
                // Notify that widget loading is complete
                WidgetsLoadCompleted?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _isLoading = false;
            }
        }

        // Initialize widgets from the factory
        private void InitializeWidgets()
        {
            // Clear existing widgets
            _widgets.Clear();
            
            // Get all available widget samples from factory
            var widgetSamples = WidgetFactory.GetAllWidgetSamples();
            
            // Add them to main collection
            foreach (var widget in widgetSamples)
            {
                _widgets.Add(widget);
            }
            
            // Load from settings if available, otherwise use defaults
            if (!LoadWidgetConfigFromSettings() && !_isInitialized)
            {
                _enabledWidgets.Clear();
                _disabledWidgets.Clear();
                
                // By default, enable the first few widgets on first run
                if (widgetSamples.Count > 0 && _enabledWidgets.Count == 0)
                {
                    // Enable Weekly Study Time widget
                    EnableWidget(widgetSamples[0]);
                    
                    // Enable Retention widget if available
                    if (widgetSamples.Count > 2)
                    {
                        EnableWidget(widgetSamples[2]);
                    }
                    
                    // Save initial configuration to settings
                    SaveWidgetConfigToSettings();
                }
                
                _isInitialized = true;
            }
        }
        
        // Load widget configuration from settings
        private bool LoadWidgetConfigFromSettings()
        {
            var settings = AppSettings.Instance;
            if (settings.EnabledWidgets == null || settings.EnabledWidgets.Count == 0)
                return false;
                
            _enabledWidgets.Clear();
            _disabledWidgets.Clear();
            
            // Sort widgets by display order
            var orderedWidgetConfigs = settings.EnabledWidgets
                .OrderBy(w => w.DisplayOrder)
                .ToList();
                
            foreach (var widgetConfig in orderedWidgetConfigs)
            {
                if (widgetConfig.IsEnabled)
                {
                    var widget = GetWidgetByType(widgetConfig.Type);
                    if (widget != null)
                    {
                        widget.IsEnabled = true;
                        widget.DisplayOrder = widgetConfig.DisplayOrder;
                        _enabledWidgets.Add(widget);
                    }
                }
            }
            
            // Make sure all widgets that aren't enabled are in the disabled collection
            foreach (var widget in _widgets)
            {
                if (!_enabledWidgets.Any(w => w.Type == widget.Type))
                {
                    // Widget isn't in the enabled collection, make sure it's disabled
                    widget.IsEnabled = false;
                    
                    // Add to disabled collection if not already there
                    if (!_disabledWidgets.Any(w => w.Type == widget.Type))
                    {
                        _disabledWidgets.Add(widget);
                    }
                }
            }
            
            return true;
        }
        
        // Save widget configuration to settings
        private void SaveWidgetConfigToSettings()
        {
            var settings = AppSettings.Instance;
            
            // Convert enabled widgets to widget config
            settings.EnabledWidgets = _enabledWidgets
                .Select(w => new WidgetConfig
                {
                    Type = w.Type,
                    DisplayOrder = w.DisplayOrder,
                    IsEnabled = w.IsEnabled
                })
                .OrderBy(w => w.DisplayOrder)
                .ToList();
                
            // Save settings
            settings.Save();
        }
        
        // Save widget configuration to settings asynchronously
        private async Task SaveWidgetConfigToSettingsAsync()
        {
            var settings = AppSettings.Instance;
            
            // Convert enabled widgets to widget config
            settings.EnabledWidgets = _enabledWidgets
                .Select(w => new WidgetConfig
                {
                    Type = w.Type,
                    DisplayOrder = w.DisplayOrder,
                    IsEnabled = w.IsEnabled
                })
                .OrderBy(w => w.DisplayOrder)
                .ToList();
                
            // Save settings asynchronously
            await settings.SaveAsync();
        }
        
        // Get a new instance of a specific widget type
        public Widget CreateWidget(WidgetType type)
        {
            return WidgetFactory.CreateWidget(type);
        }

        // Enable a widget and add it to the enabled collection
        public void EnableWidget(Widget widget)
        {
            if (widget == null) return;
            
            // Check if we've reached the maximum number of widgets
            if (_enabledWidgets.Count >= MaxWidgetsAllowed)
                return;

            widget.IsEnabled = true;
            
            if (!_enabledWidgets.Contains(widget))
            {
                _enabledWidgets.Add(widget);
                UpdateDisplayOrders();
            }
            
            if (_disabledWidgets.Contains(widget))
            {
                _disabledWidgets.Remove(widget);
            }
            
            // Save changes to settings
            SaveWidgetConfigToSettings();
        }

        // Disable a widget and move it to the disabled collection
        public void DisableWidget(Widget widget)
        {
            if (widget == null) return;

            widget.IsEnabled = false;
            
            if (_enabledWidgets.Contains(widget))
            {
                _enabledWidgets.Remove(widget);
                UpdateDisplayOrders();
            }
            
            if (!_disabledWidgets.Contains(widget))
            {
                _disabledWidgets.Add(widget);
            }
            
            // Save changes to settings
            SaveWidgetConfigToSettings();
        }

        // Move a widget up in the enabled widgets order
        public void MoveWidgetUp(Widget widget)
        {
            if (widget == null || !widget.IsEnabled) return;
            
            int index = _enabledWidgets.IndexOf(widget);
            if (index > 0)
            {
                _enabledWidgets.Move(index, index - 1);
                UpdateDisplayOrders();
                
                // Save changes to settings
                SaveWidgetConfigToSettings();
            }
        }

        // Move a widget down in the enabled widgets order
        public void MoveWidgetDown(Widget widget)
        {
            if (widget == null || !widget.IsEnabled) return;
            
            int index = _enabledWidgets.IndexOf(widget);
            if (index < _enabledWidgets.Count - 1)
            {
                _enabledWidgets.Move(index, index + 1);
                UpdateDisplayOrders();
                
                // Save changes to settings
                SaveWidgetConfigToSettings();
            }
        }

        // Update display orders based on current position in the collection
        private void UpdateDisplayOrders()
        {
            for (int i = 0; i < _enabledWidgets.Count; i++)
            {
                _enabledWidgets[i].DisplayOrder = i;
            }
        }

        // Get widget by type
        public Widget GetWidgetByType(WidgetType type)
        {
            return _widgets.FirstOrDefault(w => w.Type == type);
        }
        
        // Refresh all widgets from the Widgets folder
        public void RefreshWidgets()
        {
            // Store currently enabled widgets by type and display order
            var enabledWidgetsInfo = _enabledWidgets
                .Select(w => new { Type = w.Type, Order = w.DisplayOrder })
                .OrderBy(w => w.Order)
                .ToList();
            
            // Keep track of types that were present in the disabled collection
            var disabledTypes = _disabledWidgets.Select(w => w.Type).ToList();
            
            // Clear the widget collections
            _widgets.Clear();
            _enabledWidgets.Clear();
            _disabledWidgets.Clear();
            
            // Force scanning widget types again in case new ones were added
            WidgetFactory.RescanWidgetTypes();
            
            // Get all available widget samples from factory
            var widgetSamples = WidgetFactory.GetAllWidgetSamples();
            
            // Re-add all widgets to the main collection
            foreach (var widget in widgetSamples)
            {
                _widgets.Add(widget);
            }
            
            // Re-enable previously enabled widgets in their original order
            foreach (var widgetInfo in enabledWidgetsInfo)
            {
                var widget = _widgets.FirstOrDefault(w => w.Type == widgetInfo.Type);
                if (widget != null)
                {
                    EnableWidget(widget);
                }
            }
            
            // Make sure all non-enabled widgets are in the disabled collection
            foreach (var widget in _widgets)
            {
                if (!_enabledWidgets.Any(w => w.Type == widget.Type))
                {
                    // Widget isn't in the enabled collection, add to disabled
                    widget.IsEnabled = false;
                    
                    if (!_disabledWidgets.Any(w => w.Type == widget.Type))
                    {
                        _disabledWidgets.Add(widget);
                    }
                }
            }
            
            // Make sure display orders are updated
            UpdateDisplayOrders();
            
            // Save changes to settings
            SaveWidgetConfigToSettings();
        }
    }
} 