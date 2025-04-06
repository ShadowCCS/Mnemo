using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MnemoProject.Widgets.WeeklyStudyTime
{
    public partial class WeeklyStudyTimeView : UserControl
    {
        public WeeklyStudyTimeView()
        {
            InitializeComponent();
            
            // If DataContext isn't set, create a new view model
            if (DataContext == null)
            {
                DataContext = new WeeklyStudyTime();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 