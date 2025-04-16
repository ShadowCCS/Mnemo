using Avalonia.Controls;
using Avalonia.Input;
using MnemoProject.ViewModels;

namespace MnemoProject.Views
{
    public partial class FlashcardPracticeView : UserControl
    {
        public FlashcardPracticeView()
        {
            InitializeComponent();
            
            // Add keyboard handling
            this.KeyDown += OnKeyDown;
            this.Focusable = true;
            this.Focus();
        }
        
        // Note: We're no longer using the OnCardClick method as we've removed the click handler
        // from the UI. Using the Hide Answer button and rating buttons instead.
        
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is FlashcardPracticeViewModel viewModel)
            {
                switch (e.Key)
                {
                    case Key.Space:
                        // When spacebar is pressed, rate as "Good" (this aligns with the "Spacebar to continue" hint)
                        viewModel.RateDifficultyCommand.Execute("Good");
                        e.Handled = true;
                        break;
                        
                    case Key.Left:
                        viewModel.PreviousCardCommand.Execute(null);
                        e.Handled = true;
                        break;
                        
                    case Key.Right:
                        viewModel.NextCardCommand.Execute(null);
                        e.Handled = true;
                        break;
                        
                    case Key.H:
                        // 'H' key to toggle answer visibility
                        viewModel.FlipCardCommand.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
        }
    }
} 