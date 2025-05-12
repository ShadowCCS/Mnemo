using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels;

namespace MnemoProject.Views;

public partial class FlashcardsView : UserControl
{
    private ScrollViewer _cardsScrollViewer;
    
    public FlashcardsView()
    {
        InitializeComponent();
        
        // Get a reference to the ScrollViewer
        _cardsScrollViewer = this.FindControl<ScrollViewer>("CardsScrollViewer");
        
        // Register for the scroll changed event
        if (_cardsScrollViewer != null)
        {
            _cardsScrollViewer.ScrollChanged += CardsScrollViewer_ScrollChanged;
        }
    }
    
    private void CardsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Forward the event to the view model
        if (DataContext is FlashcardsViewModel viewModel)
        {
            viewModel.OnScrollChanged(sender, e);
        }
    }
}