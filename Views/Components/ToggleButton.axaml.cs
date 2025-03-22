using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace MnemoProject.Views.Components;

public class ToggleButton : TemplatedControl
{
    // Define dependency properties
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ToggleButton, bool>(nameof(IsChecked));

    public static readonly StyledProperty<IBrush> CheckedBackgroundProperty =
        AvaloniaProperty.Register<ToggleButton, IBrush>(nameof(CheckedBackground), new SolidColorBrush(Color.Parse("#2B92BE")));

    public static readonly StyledProperty<IBrush> UncheckedBackgroundProperty =
        AvaloniaProperty.Register<ToggleButton, IBrush>(nameof(UncheckedBackground), new SolidColorBrush(Color.Parse("#E0E0E0")));

    // Property wrappers
    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public IBrush CheckedBackground
    {
        get => GetValue(CheckedBackgroundProperty);
        set => SetValue(CheckedBackgroundProperty, value);
    }

    public IBrush UncheckedBackground
    {
        get => GetValue(UncheckedBackgroundProperty);
        set => SetValue(UncheckedBackgroundProperty, value);
    }

    // Event for notifying when IsChecked changes
    public event EventHandler<RoutedEventArgs> Checked;
    public event EventHandler<RoutedEventArgs> Unchecked;
    public event EventHandler<RoutedEventArgs> Toggled;

    // Constructor
    public ToggleButton()
    {
        this.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    // Handle pointer pressed to toggle the state
    private void OnPointerPressed(object sender, RoutedEventArgs e)
    {
        Toggle();
        e.Handled = true;
    }

    // Toggle the button state
    public void Toggle()
    {
        IsChecked = !IsChecked;
        RaiseToggleEvents();
    }

    // Raise appropriate events based on current state
    private void RaiseToggleEvents()
    {
        var eventArgs = new RoutedEventArgs();

        if (IsChecked)
        {
            Checked?.Invoke(this, eventArgs);
        }
        else
        {
            Unchecked?.Invoke(this, eventArgs);
        }

        Toggled?.Invoke(this, eventArgs);
    }

    // Override property changed to handle visual state changes
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            PseudoClasses.Set(":checked", IsChecked);
        }
    }
}