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

        [ObservableProperty]
        private ListItemTemplate? _selectedSidebarItem;

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

            AppVersion = VersionInfo.Version;

            if (!_notificationSent)
            {
                LogService.Log.Info("Showing welcome notification");
                NotificationService.Info($"You are running {AppVersion}", "Welcome To Mnemo!");
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

        partial void OnSelectedSidebarItemChanged(ListItemTemplate? value)
        {
            if (value != null)
            {
                LogService.Log.Debug($"Selected sidebar item: {value.Label}");
                // Create instance with navigation service
                var constructor = value.ModelType.GetConstructor(new[] { typeof(NavigationService) });
                if (constructor != null)
                {
                    var viewModel = constructor.Invoke(new object[] { _navigationService }) as ViewModelBase;
                    if (viewModel != null)
                    {
                        _navigationService.NavigateTo(viewModel);
                    }
                }
                else
                {
                    // Fallback to default constructor
                    var instance = Activator.CreateInstance(value.ModelType);
                    if (instance is ViewModelBase viewModel)
                    {
                        _navigationService.NavigateTo(viewModel);
                    }
                }
            }
        }

        public ObservableCollection<object> Items { get; } = new ObservableCollection<object>
        {
            new LabelItem("Main Hub"),
            new ListItemTemplate(typeof(DashboardViewModel), "DashboardIcon"),
            new ListItemTemplate(typeof(LearningPathViewModel), "LearningPathIcon"),
            new ListItemTemplate(typeof(NotesViewModel), "NotesIcon"),
            new ListItemTemplate(typeof(FlashcardsViewModel), "FlashcardsIcon"),
            new ListItemTemplate(typeof(QuizzesViewModel), "QuizzesIcon"),
            new LabelItem("Extra Content"),
            new ListItemTemplate(typeof(GamesViewModel), "GamesIcon"),
            new ListItemTemplate(typeof(ExtensionsViewModel), "ExtensionsIcon"),
            new LabelItem("Utility & Personalization"),
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
            return Regex.Replace(name, "([A-Z])", " $1").Trim();
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