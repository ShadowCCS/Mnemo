using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Generic;
using MnemoProject.Services;
using MnemoProject.Views.Overlays;

namespace MnemoProject.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private static bool _notificationSent = false;
        private string _appVersion = string.Empty;

        [ObservableProperty]
        private bool isSidebarExpanded = true;

        public ViewModelBase? CurrentPage => _navigationService.CurrentView;
        public NavigationService NavigationService => _navigationService;

        [ObservableProperty]
        private ListItemTemplate? _selectedSidebarItem;

        [ObservableProperty]
        private int _selectedIndex = 1;

        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        public MainWindowViewModel()
        {
            LogService.Log.Info("Initializing MainWindowViewModel");
            _navigationService = new NavigationService();
            _navigationService.NavigationChanged += () => OnPropertyChanged(nameof(CurrentPage));

            _navigationService.NavigateTo(new DashboardViewModel(_navigationService));

            // Initialize dashboard as the selected item
            SelectedIndex = 1; // This is the Dashboard item
            SelectedSidebarItem = Items[1] as ListItemTemplate;
            
            AppVersion = VersionInfo.Version;

            if (!_notificationSent)
            {
                LogService.Log.Info("Showing welcome notification");
                NotificationService.Info(
                    LocalizationService.Instance.GetString("MainWindow_Welcome_Message", "You are running {0}").Replace("{0}", AppVersion), 
                    LocalizationService.Instance.GetString("MainWindow_Welcome_Title", "Welcome To Mnemo!"));
                _notificationSent = true;
            }
        }

        [RelayCommand]
        public void CloseButtonCommand()
        {
            LogService.Log.Info("Closing application");
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.Shutdown();
            }
        }

        [RelayCommand]
        public void MinimizeButtonCommand(object? window)
        {
            if (window is Window win)
            {
                LogService.Log.Debug("Minimizing window");
                win.WindowState = WindowState.Minimized;
            }
        }

        [RelayCommand]
        public void FullscreenButtonCommand(object? window)
        {
            if (window is Window win)
            {
                LogService.Log.Debug("Toggling fullscreen");
                if (win.WindowState == WindowState.Normal)
                {
                    win.WindowState = WindowState.Maximized;
                }
                else
                {
                    win.WindowState = WindowState.Normal;
                }
            }
        }

        [RelayCommand]
        private void ShowShortcutsOverlay()
        {
            LogService.Log.Debug("Showing shortcuts overlay");
            var shortcutsOverlay = new ShortcutsOverlay();
            OverlayService.Instance.ShowOverlay(shortcutsOverlay);
        }

        partial void OnSelectedSidebarItemChanged(ListItemTemplate? value)
        {
            if (value != null)
            {
                LogService.Log.Debug($"Selected sidebar item: {value.Label}, ModelType: {value.ModelType.Name}");
                
                // Create instance with navigation service
                var constructor = value.ModelType.GetConstructor(new[] { typeof(NavigationService) });
                LogService.Log.Debug($"Constructor found: {constructor != null}");
                
                if (constructor != null)
                {
                    try
                    {
                        var viewModel = constructor.Invoke(new object[] { _navigationService }) as ViewModelBase;
                        LogService.Log.Debug($"ViewModel created: {viewModel != null}");
                        
                        if (viewModel != null)
                        {
                            _navigationService.NavigateTo(viewModel);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log.Error($"Error creating ViewModel: {ex.Message}");
                    }
                }
                else
                {
                    // Fallback to default constructor
                    try
                    {
                        var instance = Activator.CreateInstance(value.ModelType);
                        LogService.Log.Debug($"Instance created with default constructor: {instance != null}");
                        
                        if (instance is ViewModelBase viewModel)
                        {
                            _navigationService.NavigateTo(viewModel);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log.Error($"Error creating ViewModel with default constructor: {ex.Message}");
                    }
                }
            }
            else
            {
                LogService.Log.Debug("Selected sidebar item is null");
            }
        }

        public ObservableCollection<object> Items { get; } = new ObservableCollection<object>
        {
            new LabelItem(LocalizationService.Instance.GetString("MainWindow_SidebarLabel_MainHub", "Main Hub")),
            new ListItemTemplate(typeof(DashboardViewModel), "DashboardIcon"),
            new ListItemTemplate(typeof(LearningPathViewModel), "LearningPathIcon"),
            new ListItemTemplate(typeof(NotesViewModel), "NotesIcon"),
            new ListItemTemplate(typeof(FlashcardsViewModel), "FlashcardsIcon"),
            new ListItemTemplate(typeof(QuizzesViewModel), "QuizzesIcon"),
            new LabelItem(LocalizationService.Instance.GetString("MainWindow_SidebarLabel_ExtraContent", "Extra Content")),
            new ListItemTemplate(typeof(GamesViewModel), "GamesIcon"),
            new ListItemTemplate(typeof(ExtensionsViewModel), "ExtensionsIcon"),
            new LabelItem(LocalizationService.Instance.GetString("MainWindow_SidebarLabel_Utility", "Utility & Personalization")),
            new ListItemTemplate(typeof(SettingsViewModel), "SettingsIcon"),
        };

        [RelayCommand]
        private void TriggerSidebar()
        {
            LogService.Log.Debug($"Toggling sidebar: {(IsSidebarExpanded ? "collapsing" : "expanding")}");
            IsSidebarExpanded = !IsSidebarExpanded;
        }

        [RelayCommand]
        public void GoBack()
        {
            LogService.Log.Debug("Navigating back");
            _navigationService.GoBack();
        }

        [RelayCommand]
        public void NavigateTo(ViewModelBase viewModel)
        {
            LogService.Log.Debug($"Navigating to {viewModel.GetType().Name}");
            _navigationService.NavigateTo(viewModel);
        }
    }

    public class ListItemTemplate
    {
        public string Label { get; }
        public Type ModelType { get; }
        public StreamGeometry ListItemIcon { get; }

        public ListItemTemplate(Type type, string iconKey)
        {
            ModelType = type;
            Label = FormatLabel(type.Name.Replace("ViewModel", ""));
            if (Application.Current?.TryFindResource(iconKey, out var res) == true && res is StreamGeometry geometry)
            {
                ListItemIcon = geometry;
            }
            else
            {
                ListItemIcon = new StreamGeometry();
                LogService.Log.Warning($"Icon resource '{iconKey}' not found for {type.Name}");
            }
        }

        private string FormatLabel(string name)
        {
            // Try to get localized string for menu item
            string localizedKey = $"Menu_{name}";
            string localizedValue = LocalizationService.Instance.GetString(localizedKey, "");
            
            // If we don't have a translation, format the class name (fallback)
            if (string.IsNullOrEmpty(localizedValue))
            {
                return Regex.Replace(name, "([A-Z])", " $1").Trim();
            }
            
            return localizedValue;
        }
    }

    public class LabelItem
    {
        public string Label { get; }

        public LabelItem(string label)
        {
            Label = label;
        }
    }
}