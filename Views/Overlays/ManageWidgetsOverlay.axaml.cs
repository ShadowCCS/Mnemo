using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels.Overlays;

namespace MnemoProject.Views.Overlays
{
    public partial class ManageWidgetsOverlay : UserControl
    {
        public ManageWidgetsOverlay()
        {
            InitializeComponent();
            
            // If DataContext isn't set, create a new view model
            if (DataContext == null)
            {
                DataContext = new ManageWidgetsOverlayViewModel();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 