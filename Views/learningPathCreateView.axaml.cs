using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels;
using System;

namespace MnemoProject.Views;

public partial class LearningPathCreateView : UserControl
{
    private StackPanel _loadingView;
    private StackPanel _contentView;
    
    public LearningPathCreateView()
    {
        InitializeComponent();
        
        _loadingView = this.FindControl<StackPanel>("LoadingView");
        _contentView = this.FindControl<StackPanel>("ContentView");
        
        // Hook up to data context changed to properly connect event handlers
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object sender, EventArgs e)
    {
        // Disconnect previous handler if any
        if (DataContext is LearningPathCreateViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        // Connect to new view model
        if (DataContext is LearningPathCreateViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // Initialize visibility based on current state
            UpdateVisibility(viewModel.Units.Count > 0);
        }
    }
    
    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is LearningPathCreateViewModel viewModel)
        {
            if (e.PropertyName == nameof(LearningPathCreateViewModel.Units))
            {
                // Update visibility when units collection changes
                UpdateVisibility(viewModel.Units.Count > 0);
            }
        }
    }
    
    private void UpdateVisibility(bool hasUnits)
    {
        if (_loadingView != null && _contentView != null)
        {
            _loadingView.IsVisible = !hasUnits;
            _contentView.IsVisible = hasUnits;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}