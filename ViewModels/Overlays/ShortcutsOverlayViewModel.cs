using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Services;

namespace MnemoProject.ViewModels.Overlays
{
    public partial class ShortcutsOverlayViewModel : ViewModelBase
    {
        public ShortcutsOverlayViewModel()
        {
            // Constructor logic if needed
        }

        [RelayCommand]
        private void Close()
        {
            // Close the overlay
            OverlayService.Instance.CloseOverlay();
        }
    }
} 