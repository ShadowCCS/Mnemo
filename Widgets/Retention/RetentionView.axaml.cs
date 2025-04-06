using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MnemoProject.Widgets.Retention
{
    public partial class RetentionView : UserControl
    {
        public RetentionView()
        {
            InitializeComponent();
            
            // If DataContext isn't set, create a new view model
            if (DataContext == null)
            {
                DataContext = new Retention();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 