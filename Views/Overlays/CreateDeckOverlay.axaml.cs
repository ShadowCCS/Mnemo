using Avalonia.Controls;
using MnemoProject.Models;
using MnemoProject.ViewModels.Overlays;

namespace MnemoProject.Views.Overlays
{
    public partial class CreateDeckOverlay : UserControl
    {
        public CreateDeckOverlay()
        {
            InitializeComponent();
            DataContext = new CreateDeckOverlayViewModel();
        }
        
        public CreateDeckOverlay(Flashcard flashcard)
        {
            InitializeComponent();
            DataContext = new CreateDeckOverlayViewModel(flashcard);
        }
    }
} 