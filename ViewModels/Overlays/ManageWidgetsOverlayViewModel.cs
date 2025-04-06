using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Models;
using MnemoProject.Services;
using System.Collections.ObjectModel;

namespace MnemoProject.ViewModels.Overlays
{
    public partial class ManageWidgetsOverlayViewModel : ViewModelBase
    {
        private readonly WidgetService _widgetService;
        
        public ObservableCollection<Widget> EnabledWidgets => _widgetService.EnabledWidgets;
        public ObservableCollection<Widget> DisabledWidgets => _widgetService.DisabledWidgets;
        
        // Expose widget limit properties
        public int MaxWidgets => _widgetService.MaxWidgets;
        public bool CanAddMoreWidgets => _widgetService.CanAddMoreWidgets;

        public ManageWidgetsOverlayViewModel()
        {
            _widgetService = WidgetService.Instance;
            
            // Refresh widgets to ensure all are loaded
            _widgetService.RefreshWidgets();
        }

        [RelayCommand]
        private void Close()
        {
            // Make sure changes are reflected immediately in the Dashboard
            _widgetService.RefreshWidgets();
            
            // Close the overlay
            OverlayService.Instance.CloseOverlay();
        }

        [RelayCommand]
        private void EnableWidget(Widget widget)
        {
            if (!_widgetService.CanAddMoreWidgets)
                return;
                
            _widgetService.EnableWidget(widget);
            
            // Notify that properties have changed
            OnPropertyChanged(nameof(EnabledWidgets));
            OnPropertyChanged(nameof(DisabledWidgets));
            OnPropertyChanged(nameof(CanAddMoreWidgets));
        }

        [RelayCommand]
        private void DisableWidget(Widget widget)
        {
            _widgetService.DisableWidget(widget);
            
            // Notify that properties have changed
            OnPropertyChanged(nameof(EnabledWidgets));
            OnPropertyChanged(nameof(DisabledWidgets));
            OnPropertyChanged(nameof(CanAddMoreWidgets));
        }

        [RelayCommand]
        private void MoveWidgetUp(Widget widget)
        {
            _widgetService.MoveWidgetUp(widget);
        }

        [RelayCommand]
        private void MoveWidgetDown(Widget widget)
        {
            _widgetService.MoveWidgetDown(widget);
        }
    }
} 