using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace MnemoProject.Views.Components
{
    public class UnitButton : Button
    {
        public static readonly StyledProperty<int> UnitProperty =
            AvaloniaProperty.Register<UnitButton, int>(
                nameof(Unit));

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<UnitButton, string>(
                nameof(Text), "");

        public int Unit
        {
            get => GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    }
}