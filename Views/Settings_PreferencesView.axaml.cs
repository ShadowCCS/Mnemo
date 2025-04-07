using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels;
using System.Diagnostics;

namespace MnemoProject.Views;

public partial class Settings_PreferencesView : UserControl
{
    private ComboBox _defaultExportFormatComboBox;
    private ComboBox _autoPlayModeComboBox;
    private ComboBox _quietHoursStartComboBox;
    private ComboBox _quietHoursEndComboBox;

    public Settings_PreferencesView()
    {
        InitializeComponent();
        
        this.AttachedToVisualTree += Settings_PreferencesView_AttachedToVisualTree;
        this.DataContextChanged += Settings_PreferencesView_DataContextChanged;
    }

    private void Settings_PreferencesView_DataContextChanged(object sender, System.EventArgs e)
    {
        Debug.WriteLine("DataContext changed in Settings_PreferencesView");
        InitializeControlsIfReady();
    }

    private void Settings_PreferencesView_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        Debug.WriteLine("Settings_PreferencesView attached to visual tree");
        // Find ComboBoxes by traversing visual tree
        _defaultExportFormatComboBox = this.FindControl<ComboBox>("DefaultExportFormatComboBox");
        _autoPlayModeComboBox = this.FindControl<ComboBox>("AutoPlayModeComboBox");
        _quietHoursStartComboBox = this.FindControl<ComboBox>("QuietHoursStartComboBox");
        _quietHoursEndComboBox = this.FindControl<ComboBox>("QuietHoursEndComboBox");
        
        InitializeControlsIfReady();
    }
    
    private void InitializeControlsIfReady()
    {
        // If we have access to the ViewModel, call the initialization method
        if (DataContext is Settings_PreferencesViewModel viewModel && 
            _defaultExportFormatComboBox != null && 
            _autoPlayModeComboBox != null &&
            _quietHoursStartComboBox != null &&
            _quietHoursEndComboBox != null)
        {
            Debug.WriteLine("Initializing ComboBox selections in Preferences View");
            
            InitializeComboBoxSelection(_defaultExportFormatComboBox, viewModel.Settings.DefaultExportFormat);
            InitializeComboBoxSelection(_autoPlayModeComboBox, viewModel.Settings.AutoPlayMode);
            InitializeComboBoxSelection(_quietHoursStartComboBox, viewModel.Settings.QuietHoursStart);
            InitializeComboBoxSelection(_quietHoursEndComboBox, viewModel.Settings.QuietHoursEnd);
            
            Debug.WriteLine("ComboBox selections initialized");
        }
    }
    
    private void InitializeComboBoxSelection(ComboBox comboBox, string targetValue)
    {
        if (comboBox != null && !string.IsNullOrEmpty(targetValue))
        {
            Debug.WriteLine($"Looking for {targetValue} in ComboBox items");
            foreach (ComboBoxItem item in comboBox.Items)
            {
                string itemContent = item.Content?.ToString();
                if (itemContent == targetValue)
                {
                    Debug.WriteLine($"Found matching item: {itemContent}");
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }
}