using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MnemoProject.Services;

namespace MnemoProject.Views.Controls
{
    public partial class OverlayControl : UserControl
    {
        public OverlayControl()
        {
            InitializeComponent();
            DataContext = OverlayService.Instance;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void OverlayBackground_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Close the overlay when the user clicks on the background
            OverlayService.Instance.CloseOverlay();
            e.Handled = true;
        }
    }
} 