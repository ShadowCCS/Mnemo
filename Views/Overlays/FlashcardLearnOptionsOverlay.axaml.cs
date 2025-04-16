using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MnemoProject.Models;
using MnemoProject.Services;
using MnemoProject.ViewModels.Overlays;
using MnemoProject.ViewModels;
using MnemoProject.Views;

namespace MnemoProject.Views.Overlays
{
    public partial class FlashcardLearnOptionsOverlay : UserControl
    {
        public FlashcardLearnOptionsOverlay()
        {
            InitializeComponent();
        }

        public FlashcardLearnOptionsOverlay(Flashcard flashcard)
        {
            InitializeComponent();

            // Get the NavigationService from MainWindowViewModel
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = (MainWindow)desktop.MainWindow;
                var navigationService = ((MainWindowViewModel)mainWindow.DataContext).NavigationService;
                DataContext = new FlashcardLearnOptionsOverlayViewModel(flashcard, navigationService);
            }
        }
    }
}
