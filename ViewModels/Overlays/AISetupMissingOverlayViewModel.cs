using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Services;

namespace MnemoProject.ViewModels.Overlays
{
    public partial class AISetupMissingOverlayViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool _isGuideVisible;

        public AISetupMissingOverlayViewModel()
        {
            // Constructor logic if needed
        }

        [RelayCommand]
        private void Close()
        {
            // Close the overlay using the correct service
            OverlayService.Instance.CloseOverlay();
        }

        [RelayCommand]
        private void ToggleGuide()
        {
            IsGuideVisible = !IsGuideVisible;
        }
    }
} 