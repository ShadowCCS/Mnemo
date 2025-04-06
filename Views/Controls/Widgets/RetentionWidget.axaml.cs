using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MnemoProject.Models;

namespace MnemoProject.Views.Controls.Widgets
{
    public partial class RetentionWidget : UserControl
    {
        public RetentionWidget()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 