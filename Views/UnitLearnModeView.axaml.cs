using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels;
using Avalonia.Media;
using System.Linq;
using System.ComponentModel;
using Avalonia.VisualTree;
using System;

namespace MnemoProject.Views
{
    public partial class UnitLearnModeView : UserControl
    {
        private Button _lastSelectedButton;
        private UnitLearnModeViewModel _viewModel;
        private bool _hasBeenActivated = false;
        private bool _isAttached = false;
        
        public UnitLearnModeView()
        {
            InitializeComponent();
            
            AttachedToVisualTree += OnAttachedToVisualTree;
            DetachedFromVisualTree += OnDetachedFromVisualTree;
            
            // Ensure error state button is set up properly at initialization
            DataContextChanged += OnDataContextChanged;
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void OnDataContextChanged(object sender, EventArgs e)
        {
            // Clean up previous event handler if it exists
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _hasBeenActivated = false;
            }
            
            // Set up new event handler
            if (DataContext is UnitLearnModeViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // Only activate if already attached to visual tree
                if (_isAttached && !_hasBeenActivated)
                {
                    System.Diagnostics.Debug.WriteLine("DataContextChanged: Activating ViewModel");
                    _hasBeenActivated = true;
                    _ = _viewModel.OnActivated();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DataContextChanged: ViewModel set but not activated yet");
                }
            }
        }
        
        private void OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            _isAttached = true;
            if (DataContext is UnitLearnModeViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // Only activate once
                if (!_hasBeenActivated)
                {
                    System.Diagnostics.Debug.WriteLine("AttachedToVisualTree: Activating ViewModel");
                    _hasBeenActivated = true;
                    _ = _viewModel.OnActivated();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AttachedToVisualTree: ViewModel already activated, skipping");
                }
            }
        }
        
        private void OnDetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            _isAttached = false;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                // Do not reset _hasBeenActivated here as the view might be temporarily detached
            }
        }
        
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Handle different property changes
            switch (e.PropertyName)
            {
                case nameof(UnitLearnModeViewModel.CurrentQuestion):
                    ResetButtonHighlights();
                    break;
                    
                case nameof(UnitLearnModeViewModel.IsLoading):
                case nameof(UnitLearnModeViewModel.LoadingStatus):
                    // These properties affect the loading state UI
                    break;
            }
        }
        
        private void ResetButtonHighlights()
        {
            _lastSelectedButton = null;
            
            // Find all fill-in-blank buttons and reset their border color
            var fillInBlankGrid = this.FindControl<Grid>("FillInBlankGrid");
            if (fillInBlankGrid != null)
            {
                var buttons = fillInBlankGrid.GetVisualDescendants().OfType<Button>().ToList();
                foreach (var button in buttons)
                {
                    button.BorderBrush = new SolidColorBrush(Color.Parse("#3a3a3a"));
                }
            }
        }
        
        public void FillInBlankOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && 
                clickedButton.Tag is string optionValue && 
                DataContext is UnitLearnModeViewModel viewModel)
            {
                try
                {
                    // Reset previous button highlight
                    if (_lastSelectedButton != null)
                    {
                        _lastSelectedButton.BorderBrush = new SolidColorBrush(Color.Parse("#3a3a3a"));
                    }
                    
                    // Highlight the clicked button
                    clickedButton.BorderBrush = new SolidColorBrush(Color.Parse("#2880b1"));
                    _lastSelectedButton = clickedButton;
                    
                    // Update view model
                    viewModel.UserAnswer = optionValue;
                    System.Diagnostics.Debug.WriteLine($"Fill-in-blank option selected: '{optionValue}'");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in FillInBlankOption_Click: {ex.Message}");
                }
            }
        }
        
        public void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && 
                DataContext is UnitLearnModeViewModel viewModel)
            {
                try
                {
                    // Get the index of the RadioButton within its parent
                    if (radioButton.Parent is Panel panel)
                    {
                        int index = panel.Children.IndexOf(radioButton);
                        if (index >= 0)
                        {
                            viewModel.SelectedMultipleChoiceIndex = index;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in RadioButton_Checked: {ex.Message}");
                }
            }
        }
    }
} 