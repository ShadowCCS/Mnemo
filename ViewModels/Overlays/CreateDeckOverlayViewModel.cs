using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Data;
using MnemoProject.Models;
using MnemoProject.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Avalonia.Threading;

namespace MnemoProject.ViewModels.Overlays
{
    public partial class CreateDeckOverlayViewModel : ViewModelBase
    {
        private readonly MnemoContext _dbContext;
        private readonly Flashcard _flashcard;
        private readonly bool _isEditMode;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private ObservableCollection<Deck> _cards;

        [ObservableProperty]
        private string _frontText;

        [ObservableProperty]
        private string _backText;

        [ObservableProperty]
        private string _bulkText;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _headerText;

        [ObservableProperty]
        private bool _isBulkModeActive;

        [ObservableProperty]
        private string _bulkModeButtonText = "Single Mode";

        partial void OnIsBulkModeActiveChanged(bool value)
        {
            BulkModeButtonText = value ? "Bulk Mode" : "Single Mode";
        }

        public CreateDeckOverlayViewModel()
        {
            _dbContext = new MnemoContext();
            _flashcard = new Flashcard { Id = Guid.NewGuid() };
            _isEditMode = false;
            
            HeaderText = "Create New Deck";
            Cards = new ObservableCollection<Deck>();
            FrontText = string.Empty;
            BackText = string.Empty;
        }

        public CreateDeckOverlayViewModel(Flashcard flashcard)
        {
            _dbContext = new MnemoContext();
            _flashcard = flashcard;
            _isEditMode = true;
            
            HeaderText = "Edit Deck";
            Title = flashcard.Title;
            Cards = new ObservableCollection<Deck>(_flashcard.Decks);
            FrontText = string.Empty;
            BackText = string.Empty;
        }

        [RelayCommand]
        private void Close()
        {
            OverlayService.Instance.CloseOverlay();
        }

        [RelayCommand]
        private async Task SaveDeck()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                NotificationService.Warning("Please enter a title for your deck");
                return;
            }

            if (Cards.Count == 0)
            {
                NotificationService.Warning("Please add at least one card to your deck");
                return;
            }

            try
            {
                IsBusy = true;

                // Update flashcard properties
                _flashcard.Title = Title;
                _flashcard.CardCount = Cards.Count;

                if (_isEditMode)
                {
                    // First remove all existing cards that are not in the current collection
                    var existingCards = await _dbContext.Decks
                        .Where(d => d.FlashcardId == _flashcard.Id)
                        .ToListAsync();
                    
                    var cardsToDelete = existingCards
                        .Where(existingCard => !Cards.Any(newCard => newCard.Id == existingCard.Id))
                        .ToList();
                    
                    _dbContext.Decks.RemoveRange(cardsToDelete);
                    
                    // Attach the flashcard to the context if it's not already tracked
                    var existingFlashcard = await _dbContext.Flashcards
                        .Include(f => f.Decks)
                        .FirstOrDefaultAsync(f => f.Id == _flashcard.Id);
                    
                    if (existingFlashcard != null)
                    {
                        // Update the properties
                        existingFlashcard.Title = _flashcard.Title;
                        existingFlashcard.CardCount = _flashcard.CardCount;
                    }
                    else
                    {
                        // If the flashcard doesn't exist, add it
                        _dbContext.Flashcards.Add(_flashcard);
                    }
                }
                else
                {
                    // Add new flashcard
                    _dbContext.Flashcards.Add(_flashcard);
                }

                // Ensure all cards are associated with this flashcard
                foreach (var card in Cards)
                {
                    card.FlashcardId = _flashcard.Id;
                    
                    if (card.Id == Guid.Empty)
                    {
                        card.Id = Guid.NewGuid();
                        _dbContext.Decks.Add(card);
                    }
                    else
                    {
                        // Check if the card exists in the database
                        var existingCard = await _dbContext.Decks.FindAsync(card.Id);
                        if (existingCard != null)
                        {
                            // Update the existing card
                            existingCard.Front = card.Front;
                            existingCard.Back = card.Back;
                            existingCard.FlashcardId = card.FlashcardId;
                        }
                        else
                        {
                            // Add the card if it doesn't exist
                            _dbContext.Decks.Add(card);
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();
                
                NotificationService.Success($"Deck '{Title}' has been {(_isEditMode ? "updated" : "created")} successfully!");
                OverlayService.Instance.CloseOverlay();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to save deck: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void AddCard()
        {
            if (string.IsNullOrWhiteSpace(FrontText))
            {
                NotificationService.Warning("Front side cannot be empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(BackText))
            {
                NotificationService.Warning("Back side cannot be empty");
                return;
            }

            var newCard = new Deck
            {
                Id = Guid.NewGuid(),
                Front = FrontText,
                Back = BackText,
                FlashcardId = _flashcard.Id
            };

            Cards.Add(newCard);
            
            // Clear the input fields
            FrontText = string.Empty;
            BackText = string.Empty;
        }

        [RelayCommand]
        private void DeleteCard(Deck card)
        {
            if (card != null)
            {
                Cards.Remove(card);
            }
        }

        [RelayCommand]
        private void ToggleBulkMode()
        {
            IsBulkModeActive = !IsBulkModeActive;
        }

        [RelayCommand]
        private void OpenBulkAddOverlay()
        {
            var bulkAddViewModel = new BulkAddOverlayViewModel(this);
            var bulkAddOverlay = new Views.Overlays.BulkAddOverlay
            {
                DataContext = bulkAddViewModel
            };
            
            OverlayService.Instance.ShowOverlay(bulkAddOverlay);
        }

        [RelayCommand]
        private void AddBulkCards()
        {
            if (string.IsNullOrWhiteSpace(BulkText))
            {
                NotificationService.Warning("Bulk text cannot be empty");
                return;
            }
            
            var lines = BulkText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var line in lines)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Try to split by comma
                var parts = line.Split(new[] { ',' }, 2);
                
                if (parts.Length != 2)
                {
                    failureCount++;
                    continue;
                }
                
                var front = parts[0].Trim();
                var back = parts[1].Trim();
                
                if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
                {
                    failureCount++;
                    continue;
                }
                
                var newCard = new Deck
                {
                    Id = Guid.NewGuid(),
                    Front = front,
                    Back = back,
                    FlashcardId = _flashcard.Id
                };
                
                Cards.Add(newCard);
                successCount++;
            }
            
            if (failureCount > 0)
            {
                NotificationService.Warning($"Added {successCount} cards. {failureCount} cards failed due to invalid format. Use 'front, back' format for each line.");
            }
            else if (successCount > 0)
            {
                NotificationService.Success($"Successfully added {successCount} cards");
                BulkText = string.Empty;
            }
            else
            {
                NotificationService.Warning("No valid cards found. Use 'front, back' format for each line.");
            }
        }

        // Methods to allow BulkAddOverlayViewModel to interact with this view model
        public Guid GetFlashcardId()
        {
            return _flashcard.Id;
        }
        

        public void AddCardToCollection(Deck card)
        {
            Dispatcher.UIThread.Post(() => {
                Cards.Add(card);
            });
        }
    }
} 