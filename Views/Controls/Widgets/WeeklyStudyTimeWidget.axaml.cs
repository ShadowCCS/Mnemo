using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MnemoProject.Models;

namespace MnemoProject.Views.Controls.Widgets
{
    public partial class WeeklyStudyTimeWidget : UserControl
    {
        public WeeklyStudyTimeWidget()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 