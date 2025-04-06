using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MnemoProject.Widgets.StudyGoal
{
    public partial class StudyGoalView : UserControl
    {
        public StudyGoalView()
        {
            InitializeComponent();
            
            // If DataContext isn't set, create a new view model
            if (DataContext == null)
            {
                DataContext = new StudyGoal();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 