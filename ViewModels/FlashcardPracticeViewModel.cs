using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Models;
using MnemoProject.Services;
using MnemoProject.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;

namespace MnemoProject.ViewModels
{
    public partial class FlashcardPracticeViewModel : ViewModelBase
    {
        private Flashcard _flashcard;
        private readonly NavigationService _navigationService;
        private List<Deck> _cards;
        private List<Deck> _dueCards;
        private readonly MnemoContext _dbContext;
        private readonly int _maxCardsPerSession;
        
        private int _currentIndex;
        private bool _isAutoPlayActive;
        private DispatcherTimer _autoPlayTimer;
        
        [ObservableProperty]
        private string _deckTitle;
        
        [ObservableProperty]
        private Deck _currentCard;
        
        [ObservableProperty]
        private bool _isShowingFront = true;
        
        [ObservableProperty]
        private string _progressText;
        
        [ObservableProperty]
        private double _progressPercentage;

        [ObservableProperty]
        private int _cardsLearned = 0;

        [ObservableProperty]
        private string answerToggleButtonText = "Show Answer";

        [ObservableProperty]
        private string autoPlayingButtonText = "Auto Play";

        [ObservableProperty]
        private bool _sessionCompleted = false;

        [ObservableProperty]
        private string _nextReviewInfo = string.Empty;

        // New properties for completion message
        [ObservableProperty]
        private bool _showCompletionMessage = false;

        [ObservableProperty]
        private string _completionMessage = string.Empty;

        // Add properties to control button state
        public bool CanGoNext => _dueCards.Count > 0 && _currentIndex < _dueCards.Count - 1 && !SessionCompleted;
        
        public bool CanGoPrevious => _dueCards.Count > 0 && _currentIndex > 0 && !SessionCompleted;
        
        public FlashcardPracticeViewModel(Flashcard flashcard, NavigationService navigationService)
        {
            _flashcard = flashcard;
            _navigationService = navigationService;
            _dbContext = new MnemoContext();
            
            // Get cards per session from settings with fallback to 20 if not set
            _maxCardsPerSession = AppSettings.Instance?.CardsPerSession > 0 
                                ? AppSettings.Instance.CardsPerSession 
                                : 20;
            
            // Load the deck directly from the database to ensure we have tracked entities
            LoadDecksFromDatabase(flashcard.Id);
        }
        
        private async void LoadDecksFromDatabase(Guid flashcardId)
        {
            try
            {
                // Load the flashcard with its decks directly from the database
                var loadedFlashcard = await _dbContext.Flashcards
                    .Include(f => f.Decks)
                    .FirstOrDefaultAsync(f => f.Id == flashcardId);
                    
                if (loadedFlashcard != null)
                {
                    // Update our reference to the flashcard
                    _flashcard = loadedFlashcard;
                    _cards = _flashcard.Decks.ToList();
                    
                    // Filter to only show due cards
                    var allDueCards = _cards.Where(c => c.IsDue).ToList();
                    
                    // Limit the number of due cards to the max per session
                    _dueCards = allDueCards.Take(_maxCardsPerSession).ToList();
                    
                    DeckTitle = _flashcard.Title;
                    _currentIndex = 0;
                    
                    // Set initial card
                    if (_dueCards.Count > 0)
                    {
                        CurrentCard = _dueCards[0];
                        UpdateNextReviewInfo();
                        UpdateProgress();
                        
                        // Show message if we're limiting the number of cards
                        if (allDueCards.Count > _dueCards.Count)
                        {
                            NotificationService.Info($"Showing {_dueCards.Count} of {allDueCards.Count} due cards based on your cards per session setting.");
                        }
                    }
                    else if (_cards.Count > 0)
                    {
                        // No cards due, show a message
                        NotificationService.Info("No cards due for review. Come back later!");
                        SessionCompleted = true;
                        // Show completion message
                        ShowCompletionMessage = true;
                        CompletionMessage = "You've completed all your daily reviews! Come back tomorrow for more cards.";
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to load flashcards: {ex.Message}");
            }
            
            // Initialize auto-play timer
            _autoPlayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _autoPlayTimer.Tick += AutoPlayTick;
        }
        
        [RelayCommand]
        private void FlipCard()
        {
            if (SessionCompleted) return;
            
            IsShowingFront = !IsShowingFront;
            AnswerToggleButtonText = IsShowingFront ? "Show Answer" : "Hide Answer";
        }

        [RelayCommand]
        private void NextCard()
        {
            if (SessionCompleted) return;
            
            if (CanGoNext)
            {
                _currentIndex++;
                CurrentCard = _dueCards[_currentIndex];
                UpdateNextReviewInfo();
                if (!IsShowingFront)
                {
                    IsShowingFront = true;
                    AnswerToggleButtonText = "Show Answer";
                }
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
            }
            else
            {
                // We're already at the last card
                NotificationService.Success("You're at the last card. Rate it to complete the deck.");
            }
        }

        [RelayCommand]
        private void PreviousCard()
        {
            if (SessionCompleted) return;
            
            if (CanGoPrevious)
            {
                _currentIndex--;
                CurrentCard = _dueCards[_currentIndex];
                UpdateNextReviewInfo();
                if (!IsShowingFront)
                {
                    IsShowingFront = true;
                    AnswerToggleButtonText = "Show Answer";
                }
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
            }
        }
        
        [RelayCommand]
        private void GoBack()
        {
            // Stop auto-play if active
            if (_isAutoPlayActive)
            {
                ToggleAutoPlay();
            }
            
            _navigationService.GoBack();
        }

        [RelayCommand]
        private void ToggleAutoPlay()
        {
            if (SessionCompleted) return;
            
            _isAutoPlayActive = !_isAutoPlayActive;
            
            if (_isAutoPlayActive)
            {
                _autoPlayTimer.Start();
                AutoPlayingButtonText = "Auto Playing";
            }
            else
            {
                _autoPlayTimer.Stop();
                AutoPlayingButtonText = "Auto Play";
            }
        }

        [RelayCommand]
        private async Task RateDifficultyAsync(string difficulty)
        {
            // If session is already completed, ignore rating
            if (SessionCompleted) return;
            
            // Apply the SM-2 algorithm to the current card
            int quality = SpacedRepetitionService.GetQualityFromDifficulty(difficulty);
            SpacedRepetitionService.ApplySM2Algorithm(CurrentCard, quality);
            
            // Update database
            try
            {
                // Ensure the entity is being tracked by the context
                var trackedCard = await _dbContext.Decks.FindAsync(CurrentCard.Id);
                if (trackedCard != null)
                {
                    // Update the tracked entity with the new values
                    trackedCard.EaseFactor = CurrentCard.EaseFactor;
                    trackedCard.Interval = CurrentCard.Interval;
                    trackedCard.RepetitionCount = CurrentCard.RepetitionCount;
                    trackedCard.LastReviewDate = CurrentCard.LastReviewDate;
                    trackedCard.NextReviewDate = CurrentCard.NextReviewDate;
                }
                else
                {
                    // If not found, attach and mark as modified
                    _dbContext.Attach(CurrentCard);
                    _dbContext.Entry(CurrentCard).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to save changes: {ex.Message}");
            }
            
            // Increment cards learned based on difficulty
            // Make sure we don't exceed the total number of cards
            if (CardsLearned < _dueCards.Count)
            {
                CardsLearned++;
            }
            UpdateProgress();
            
            // Show detailed feedback based on the next review date and difficulty
            if (CurrentCard.NextReviewDate.HasValue)
            {
                int daysUntilReview = (CurrentCard.NextReviewDate.Value.Date - DateTime.Today).Days;
                
                if (daysUntilReview == 0)
                {
                    NotificationService.Info("Card will be reviewed again later today");
                }
                else if (daysUntilReview == 1)
                {
                    NotificationService.Info("Card will be reviewed again tomorrow");
                }
                else
                {
                    NotificationService.Info($"Card will be reviewed again in {daysUntilReview} days");
                }
            }
            
            // Check if we're at the last card
            if (_currentIndex >= _dueCards.Count - 1)
            {
                // We're on the last card and have rated it, so mark session as completed
                SessionCompleted = true;
                NotificationService.Success("You've completed the deck!");
                
                // Show completion message
                ShowCompletionMessage = true;
                CompletionMessage = "Congratulations! You've completed all your daily reviews using the SuperMemo algorithm. Your progress has been saved, and cards will reappear based on your performance.";
                
                // Stop auto-play if it's running
                if (_isAutoPlayActive)
                {
                    ToggleAutoPlay();
                }
                
                // Update button states
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
                return; // Don't try to move to the next card
            }
            
            // If we get here, there are more cards, so move to the next one
            _currentIndex++;
            CurrentCard = _dueCards[_currentIndex];
            UpdateNextReviewInfo();
            // Reset to front side when moving to a new card
            IsShowingFront = true;
            AnswerToggleButtonText = "Show Answer";
            
            // Update navigation button states
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CanGoPrevious));
        }
        
        private void UpdateNextReviewInfo()
        {
            if (CurrentCard.NextReviewDate.HasValue)
            {
                NextReviewInfo = $"Next review: {CurrentCard.NextReviewDate.Value.ToShortDateString()}";
            }
            else
            {
                NextReviewInfo = "New card";
            }
        }
        
        private void AutoPlayTick(object sender, EventArgs e)
        {
            if (SessionCompleted)
            {
                ToggleAutoPlay();
                
                // Ensure completion message is shown in auto-play mode
                if (!ShowCompletionMessage)
                {
                    ShowCompletionMessage = true;
                    CompletionMessage = "Congratulations! You've completed all your daily reviews using the SuperMemo algorithm. Your progress has been saved, and cards will reappear based on your performance.";
                }
                return;
            }
            
            // If showing front (question only), show the answer
            // otherwise move to the next card
            if (IsShowingFront)
            {
                FlipCard();
            }
            else
            {
                // Check if we're at the last card before rating
                bool isLastCard = _currentIndex >= _dueCards.Count - 1;
                
                // Rate as "Good" when auto-playing
                RateDifficultyAsync("Good");
                
                // If we were on the last card, stop auto-playing
                if (isLastCard)
                {
                    ToggleAutoPlay();
                }
            }
        }
        
        private void UpdateProgress()
        {
            // Ensure the cards learned never exceeds the total number of cards
            CardsLearned = Math.Min(CardsLearned, _dueCards.Count);
            ProgressText = $"{CardsLearned} of {_dueCards.Count} cards learned.";
            ProgressPercentage = _dueCards.Count > 0 ? CardsLearned * 100.0 / _dueCards.Count : 0;
        }
    }
} 