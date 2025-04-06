using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MnemoProject.Widgets.LongestStreak
{
    public partial class LongestStreakView : UserControl
    {
        public LongestStreakView()
        {
            InitializeComponent();
            
            // If DataContext isn't set, create a new view model
            if (DataContext == null)
            {
                DataContext = new LongestStreak();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 