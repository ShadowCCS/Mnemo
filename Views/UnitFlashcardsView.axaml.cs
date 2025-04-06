using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels;

namespace MnemoProject.Views
{
    public partial class UnitFlashcardsView : UserControl
    {
        public UnitFlashcardsView()
        {
            InitializeComponent();
        }

        private void OnFlashcardPressed(object sender, PointerPressedEventArgs e)
        {
            if (DataContext is UnitFlashcardsViewModel viewModel)
            {
                viewModel.FlipCard();
            }
        }
    }
} 