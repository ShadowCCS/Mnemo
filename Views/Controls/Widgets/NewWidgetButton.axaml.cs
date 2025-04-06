using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MnemoProject.Views.Controls.Widgets
{
    public partial class NewWidgetButton : UserControl
    {
        public NewWidgetButton()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 