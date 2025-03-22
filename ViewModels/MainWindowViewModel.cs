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

namespace MnemoProject.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private bool isSidebarExpanded = true;

        public ViewModelBase? CurrentPage => _navigationService.CurrentView;

        [ObservableProperty]
        private ListItemTemplate? _selectedSidebarItem;


        private string _appVersion;

        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        public MainWindowViewModel()
        {
            _navigationService = new NavigationService();
            _navigationService.NavigationChanged += () => OnPropertyChanged(nameof(CurrentPage));

            _navigationService.NavigateTo(new DashboardViewModel(_navigationService));

            AppVersion = VersionInfo.Version;
        }

        [RelayCommand]
        public void CloseButtonCommand()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.Shutdown();
            }
        }

        [RelayCommand]
        public void MinimizeButtonCommand(object window)
        {
            if (window is Window win)
            {
                win.WindowState = WindowState.Minimized;
            }
        }

        [RelayCommand]
        public void FullscreenButtonCommand(object window)
        {
            if (window is Window win)
            {
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
            IsSidebarExpanded = !IsSidebarExpanded;
        }

        [RelayCommand]
        public void GoBack() => _navigationService.GoBack();

        [RelayCommand]
        public void NavigateTo(ViewModelBase viewModel) => _navigationService.NavigateTo(viewModel);
    }

    public class NavigationService
    {
        private readonly Stack<ViewModelBase> _navigationStack = new Stack<ViewModelBase>();

        public ViewModelBase? CurrentView => _navigationStack.Count > 0 ? _navigationStack.Peek() : null;

        public event Action? NavigationChanged;

        public void NavigateTo(ViewModelBase viewModel)
        {
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke();
        }

        public bool CanGoBack => _navigationStack.Count > 1;

        public void GoBack()
        {
            if (CanGoBack)
            {
                _navigationStack.Pop();
                NavigationChanged?.Invoke();
            }
        }
    }

    public class ListItemTemplate
    {
        public ListItemTemplate(Type type, string iconKey)
        {
            ModelType = type;
            Label = FormatLabel(type.Name.Replace("ViewModel", ""));
            Application.Current!.TryFindResource(iconKey, out var res);
            ListItemIcon = (StreamGeometry)res!;
        }

        public string Label { get; }
        public Type ModelType { get; }
        public StreamGeometry ListItemIcon { get; }

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