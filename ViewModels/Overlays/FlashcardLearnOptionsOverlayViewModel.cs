using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Models;
using MnemoProject.Services;
using MnemoProject.ViewModels;

namespace MnemoProject.ViewModels.Overlays
{
    public partial class FlashcardLearnOptionsOverlayViewModel : ViewModelBase
    {
        private readonly Flashcard _flashcard;
        private readonly NavigationService _navigationService;
        
        [ObservableProperty]
        private string _deckTitle;
        
        public FlashcardLearnOptionsOverlayViewModel(Flashcard flashcard, NavigationService navigationService)
        {
            _flashcard = flashcard;
            _navigationService = navigationService;
            DeckTitle = flashcard.Title;
        }
        
        [RelayCommand]
        private void Close()
        {
            OverlayService.Instance.CloseOverlay();
        }
        
        [RelayCommand]
        private void StandardPractice()
        {
            // Close the overlay first
            OverlayService.Instance.CloseOverlay();
            
            // Create the practice view model
            var practiceViewModel = new FlashcardPracticeViewModel(_flashcard, _navigationService);
            
            // Navigate to the practice view
            _navigationService.NavigateTo(practiceViewModel);
        }
        
        [RelayCommand]
        private void TimedPractice()
        {
            // This would be implemented in the future - just showing a notification for now
            NotificationService.Info("Timed practice will be available in a future update.");
        }
        
        [RelayCommand]
        private void SpacedRepetition()
        {
            // This would be implemented in the future - just showing a notification for now
            NotificationService.Info("Spaced repetition will be available in a future update.");
        }
    }
} 