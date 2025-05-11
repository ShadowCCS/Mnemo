using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MnemoProject.Models;
using MnemoProject.Services;
using MnemoProject.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace MnemoProject.ViewModels
{
    public partial class FlashcardsViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private readonly MnemoContext _dbContext;
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Flashcard> _flashcardDecks;
        private Flashcard _selectedDeck;
        private bool _hasFlashcards;
        private bool _isLoading;
        private bool _isInitialized = false;
        private Task _initializationTask = null;
        private string _deckSearchQuery;
        private string _cardSearchQuery;
        private ObservableCollection<Flashcard> _filteredDecks;
        private ObservableCollection<Deck> _filteredCards;

        public string DeckSearchQuery
        {
            get => _deckSearchQuery;
            set 
            { 
                if (SetProperty(ref _deckSearchQuery, value))
                {
                    FilterDecks();
                }
            }
        }

        public string CardSearchQuery
        {
            get => _cardSearchQuery;
            set 
            { 
                if (SetProperty(ref _cardSearchQuery, value))
                {
                    FilterCards();
                }
            }
        }

        public ObservableCollection<Flashcard> FlashcardDecks
        {
            get => _flashcardDecks;
            set => SetProperty(ref _flashcardDecks, value);
        }

        public ObservableCollection<Flashcard> FilteredDecks
        {
            get => _filteredDecks;
            set => SetProperty(ref _filteredDecks, value);
        }

        public ObservableCollection<Deck> FilteredCards
        {
            get => _filteredCards;
            set => SetProperty(ref _filteredCards, value);
        }

        public Flashcard SelectedDeck
        {
            get => _selectedDeck;
            set
            {
                if (SetProperty(ref _selectedDeck, value))
                {
                    if (value != null)
                    {
                        LoadDeckCards();
                    }
                    OnPropertyChanged(nameof(IsDeckSelected));
                    
                    // Force command bindings to refresh
                    OnPropertyChanged(nameof(EditDeckCommand));
                    OnPropertyChanged(nameof(DeleteDeckCommand));
                    OnPropertyChanged(nameof(LearnDeckCommand));
                    OnPropertyChanged(nameof(ExportDeckCommand));
                }
            }
        }

        public bool HasFlashcards
        {
            get => _hasFlashcards;
            set => SetProperty(ref _hasFlashcards, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsDeckSelected => SelectedDeck != null;

        // Commands
        public ICommand CreateDeckCommand { get; }
        public ICommand EditDeckCommand { get; }
        public ICommand DeleteDeckCommand { get; }
        public ICommand LearnDeckCommand { get; }
        public ICommand ExportDeckCommand { get; }

        // Constructor with dependency injection
        public FlashcardsViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            _dbContext = new MnemoContext();
            _databaseService = new DatabaseService();
            FlashcardDecks = new ObservableCollection<Flashcard>();
            FilteredDecks = new ObservableCollection<Flashcard>();
            FilteredCards = new ObservableCollection<Deck>();

            // Initialize commands
            CreateDeckCommand = new RelayCommand(CreateDeck);
            EditDeckCommand = new RelayCommand<Flashcard>(EditDeck, CanExecuteDeckCommand);
            DeleteDeckCommand = new RelayCommand<Flashcard>(DeleteDeck, CanExecuteDeckCommand);
            LearnDeckCommand = new RelayCommand<Flashcard>(LearnDeck, CanExecuteDeckCommand);
            ExportDeckCommand = new RelayCommand<Flashcard>(ExportDeck, CanExecuteDeckCommand);

            // Start initialization immediately in background
            _initializationTask = Task.Run(async () => 
            {
                try 
                {
                    await _databaseService.InitializeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in background initialization: {ex.Message}");
                }
            });
            
            // Then schedule UI update
            Dispatcher.UIThread.Post(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;
            
            IsLoading = true;
            try 
            {
                // Wait for background initialization to complete if it hasn't already
                if (_initializationTask != null)
                {
                    await _initializationTask;
                    _initializationTask = null;
                }
                
                await LoadFlashcardsAsync();
                _isInitialized = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadFlashcardsAsync()
        {
            try
            {
                // Run database query on background thread
                var flashcards = await Task.Run(async () => {
                    return await _dbContext.Flashcards
                        .Include(f => f.Decks)
                        .ToListAsync()
                        .ConfigureAwait(false);
                });

                // Update UI on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    FlashcardDecks.Clear();
                    foreach (var deck in flashcards)
                    {
                        FlashcardDecks.Add(deck);
                    }

                    HasFlashcards = FlashcardDecks.Count > 0;
                    
                    // Update filtered decks
                    FilterDecks();
                    
                    // Set the first deck as selected if there are any decks
                    if (HasFlashcards && FilteredDecks.Count > 0)
                    {
                        SelectedDeck = FilteredDecks.First();
                    }
                });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to load flashcards: {ex.Message}");
            }
        }

        private void FilterDecks()
        {
            FilteredDecks.Clear();
            
            var query = string.IsNullOrWhiteSpace(DeckSearchQuery) 
                ? FlashcardDecks 
                : FlashcardDecks.Where(d => d.Title.Contains(DeckSearchQuery, StringComparison.OrdinalIgnoreCase));
                
            foreach (var deck in query)
            {
                FilteredDecks.Add(deck);
            }
            
            // If the currently selected deck is no longer in the filtered list, select the first filtered deck
            if (SelectedDeck != null && !FilteredDecks.Contains(SelectedDeck) && FilteredDecks.Count > 0)
            {
                SelectedDeck = FilteredDecks.First();
            }
        }
        
        private void FilterCards()
        {
            if (SelectedDeck == null) return;
            
            FilteredCards.Clear();
            
            var query = string.IsNullOrWhiteSpace(CardSearchQuery)
                ? SelectedDeck.Decks
                : SelectedDeck.Decks.Where(c => 
                    c.Front.Contains(CardSearchQuery, StringComparison.OrdinalIgnoreCase) || 
                    c.Back.Contains(CardSearchQuery, StringComparison.OrdinalIgnoreCase));
                
            foreach (var card in query)
            {
                FilteredCards.Add(card);
            }
        }

        private async void LoadDeckCards()
        {
            if (SelectedDeck == null) return;

            try
            {
                // Create a separate context for this operation to avoid concurrent access
                using (var context = new MnemoContext())
                {
                    // Get a fresh copy of the selected deck with its cards
                    var freshDeck = await context.Flashcards
                        .Include(f => f.Decks)
                        .FirstOrDefaultAsync(f => f.Id == SelectedDeck.Id);
                    
                    if (freshDeck != null)
                    {
                        // Update the cards collection in the UI thread
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            // Update the Decks collection in the SelectedDeck
                            SelectedDeck.Decks.Clear();
                            foreach (var card in freshDeck.Decks)
                            {
                                SelectedDeck.Decks.Add(card);
                            }
                            
                            // Update filtered cards
                            FilterCards();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to load deck cards: {ex.Message}");
            }
        }

        private void CreateDeck()
        {
            // Show create deck overlay
            var createDeckView = new Views.Overlays.CreateDeckOverlay();
            OverlayService.Instance.ShowOverlay(createDeckView);
            
            // Refresh flashcards when overlay is closed
            OverlayService.Instance.OverlayClosed += async (s, e) => await LoadFlashcardsAsync();
        }

        private bool CanExecuteDeckCommand(Flashcard deck) => deck != null;

        private void EditDeck(Flashcard deck)
        {
            if (deck == null) return;

            // Show edit deck overlay
            var editDeckView = new Views.Overlays.CreateDeckOverlay(deck);
            OverlayService.Instance.ShowOverlay(editDeckView);
            
            // Refresh flashcards when overlay is closed
            OverlayService.Instance.OverlayClosed += async (s, e) => await LoadFlashcardsAsync();
        }

        private async void DeleteDeck(Flashcard deck)
        {
            if (deck == null) return;

            try
            {
                _dbContext.Flashcards.Remove(deck);
                await _dbContext.SaveChangesAsync();
                
                FlashcardDecks.Remove(deck);
                FilteredDecks.Remove(deck);
                
                HasFlashcards = FlashcardDecks.Count > 0;
                
                NotificationService.Success($"Deck '{deck.Title}' deleted successfully");
                
                if (FilteredDecks.Count > 0)
                {
                    SelectedDeck = FilteredDecks.First();
                }
                else
                {
                    SelectedDeck = null;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to delete deck: {ex.Message}");
            }
        }

        private void LearnDeck(Flashcard deck)
        {
            if (deck == null) return;

            // Show learn mode overlay with practice options
            var learnView = new Views.Overlays.FlashcardLearnOptionsOverlay(deck);
            OverlayService.Instance.ShowOverlay(learnView);
        }

        private void ExportDeck(Flashcard deck)
        {
            if (deck == null) return;

            // Implement export functionality later
            NotificationService.Info($"Export functionality for deck '{deck.Title}' will be implemented soon.");
        }
    }
}
