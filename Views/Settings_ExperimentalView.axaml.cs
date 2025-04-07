using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels;
using System.Diagnostics;

namespace MnemoProject.Views;

public partial class Settings_ExperimentalView : UserControl
{
    private ComboBox _aiProviderComboBox;
    private ComboBox _responseLanguageComboBox;

    public Settings_ExperimentalView()
    {
        InitializeComponent();
        
        this.AttachedToVisualTree += Settings_ExperimentalView_AttachedToVisualTree;
        this.DataContextChanged += Settings_ExperimentalView_DataContextChanged;
    }

    private void Settings_ExperimentalView_DataContextChanged(object sender, System.EventArgs e)
    {
        Debug.WriteLine("DataContext changed in Settings_ExperimentalView");
        InitializeControlsIfReady();
    }

    private void Settings_ExperimentalView_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        Debug.WriteLine("Settings_ExperimentalView attached to visual tree");
        // Find ComboBoxes by traversing visual tree
        _aiProviderComboBox = this.FindControl<ComboBox>("AIProviderComboBox");
        _responseLanguageComboBox = this.FindControl<ComboBox>("ResponseLanguageComboBox");
        
        InitializeControlsIfReady();
    }
    
    private void InitializeControlsIfReady()
    {
        // If we have access to the ViewModel, call the initialization method
        if (DataContext is Settings_ExperimentalViewModel viewModel && 
            _aiProviderComboBox != null && 
            _responseLanguageComboBox != null)
        {
            Debug.WriteLine($"Settings viewModel AIProvider: {viewModel.Settings.AIProvider}");
            Debug.WriteLine($"Settings viewModel ResponseLanguage: {viewModel.Settings.ResponseLanguage}");
            
            viewModel.InitializeComboBoxSelections(_aiProviderComboBox, _responseLanguageComboBox);
            
            Debug.WriteLine("ComboBox selections initialized");
        }
    }
}