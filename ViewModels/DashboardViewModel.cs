using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Models;
using MnemoProject.Services;
using MnemoProject.ViewModels.Overlays;
using MnemoProject.Views.Overlays;

namespace MnemoProject.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private readonly WidgetService _widgetService;

        public ObservableCollection<Widget> EnabledWidgets => _widgetService.EnabledWidgets;
        
        // Property to check if more widgets can be added
        public bool CanAddMoreWidgets => _widgetService.CanAddMoreWidgets;

        [ObservableProperty]
        private bool _isLoading = true;

        public DashboardViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            _widgetService = WidgetService.Instance;
            
            // Check if widgets are already loaded
            if (_widgetService.IsInitialized)
            {
                IsLoading = false;
            }
            else
            {
                // Subscribe to widgets load complete event
                _widgetService.WidgetsLoadCompleted += OnWidgetsLoadCompleted;
            }
            
            // Make sure widgets are properly loaded
            RefreshWidgets();
        }
        
        private void OnWidgetsLoadCompleted(object? sender, EventArgs e)
        {
            IsLoading = false;
            RefreshWidgets();
            
            // Unsubscribe from the event to prevent memory leaks
            _widgetService.WidgetsLoadCompleted -= OnWidgetsLoadCompleted;
        }
        
        // Refresh widgets from the service
        public void RefreshWidgets()
        {
            _widgetService.RefreshWidgets();
            OnPropertyChanged(nameof(EnabledWidgets));
            OnPropertyChanged(nameof(CanAddMoreWidgets));
        }

        [RelayCommand]
        private void OpenWidgetManagement()
        {
            var viewModel = new ManageWidgetsOverlayViewModel();
            var overlay = new ManageWidgetsOverlay { DataContext = viewModel };
            
            // Create a local event handler we can unsubscribe
            EventHandler? handler = null;
            handler = (s, e) => 
            {
                RefreshWidgets();
                // Unsubscribe after handling the event once
                OverlayService.Instance.OverlayClosed -= handler;
            };
            
            // Subscribe to overlay closed event to refresh widgets
            OverlayService.Instance.OverlayClosed += handler;
            
            OverlayService.Instance.ShowOverlay(overlay);
        }
    }
}
