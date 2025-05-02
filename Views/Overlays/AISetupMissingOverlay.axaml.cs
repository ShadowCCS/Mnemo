using Avalonia.Controls;
using Avalonia.Input;
using MnemoProject.ViewModels.Overlays;
using System.Diagnostics;
using System;

namespace MnemoProject.Views.Overlays
{
    public partial class AISetupMissingOverlay : UserControl
    {
        public AISetupMissingOverlay()
        {
            InitializeComponent();
            DataContext = new AISetupMissingOverlayViewModel();
        }

        private void OnDocumentationClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            var url = "https://platform.openai.com/docs/quickstart";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open link: {ex.Message}");
            }
        }
        
        private void OnToggleGuideClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (DataContext is AISetupMissingOverlayViewModel viewModel)
            {
                viewModel.ToggleGuideCommand.Execute(null);
            }
        }
    }
} 