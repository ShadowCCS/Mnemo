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
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.Input;
using System.Threading;

namespace MnemoProject.ViewModels
{
    public partial class FlashcardsViewModel : ViewModelBase, IDisposable
    {
        // Constants for lazy loading
        private const int PAGE_SIZE = 8;
        
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
        private Window _mainWindow;
        
        // Lazy loading properties
        private int _currentPage = 0;
        private bool _hasMoreCards = true;
        private bool _isLoadingMore = false;
        private List<Deck> _allDeckCards = new List<Deck>();
        
        // Operation cancellation tracking
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Lazy loading tracking properties
        private bool _isLoadingMoreCards;
        private string _loadingStatus = "Loading cards...";
        
        public bool IsLoadingMoreCards
        {
            get => _isLoadingMoreCards;
            set => SetProperty(ref _isLoadingMoreCards, value);
        }
        
        public string LoadingStatus
        {
            get => _loadingStatus;
            set => SetProperty(ref _loadingStatus, value);
        }

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
                        // Cancel any ongoing loading operations
                        CancelOngoingOperations();
                        
                        // Clear the cards collection immediately to prevent visual carryover
                        FilteredCards.Clear();
                        
                        // Then load the new deck's cards
                        LoadDeckCards();
                    }
                    OnPropertyChanged(nameof(IsDeckSelected));
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

        public ICommand ImportDeckCommand { get; }

        // Constructor with dependency injection
        public FlashcardsViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            _dbContext = new MnemoContext();
            _databaseService = new DatabaseService();

            // Get main window from application lifetime
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _mainWindow = desktop.MainWindow;
            }

            // Initialize properties
            FlashcardDecks = new ObservableCollection<Flashcard>();
            FilteredDecks = new ObservableCollection<Flashcard>();
            FilteredCards = new ObservableCollection<Deck>();

            // Load data asynchronously to avoid UI freeze
            _initializationTask = InitializeAsync();

            // Initialize commands
            CreateDeckCommand = new RelayCommand(CreateDeck);
            EditDeckCommand = new RelayCommand<Flashcard>(EditDeck, CanExecuteDeckCommand);
            DeleteDeckCommand = new RelayCommand<Flashcard>(DeleteDeck, CanExecuteDeckCommand);
            LearnDeckCommand = new RelayCommand<Flashcard>(LearnFlashcard, CanExecuteDeckCommand);
            ExportDeckCommand = new RelayCommand<Flashcard>(ExportFlashcard, CanExecuteDeckCommand);
            ImportDeckCommand = new RelayCommand(() => ImportFlashcard(null));
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
                // Run database query on background thread - only get basic deck info without cards
                var flashcards = await Task.Run(async () =>
                {
                    return await _dbContext.Flashcards
                        .Select(f => new Flashcard 
                        { 
                            Id = f.Id, 
                            Title = f.Title,
                            CardCount = f.Decks.Count
                        })
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

        private async void FilterCards()
        {
            if (SelectedDeck == null) return;

            // Cancel any ongoing operations first
            CancelOngoingOperations();
            
            // Reset pagination
            _currentPage = 0;
            _hasMoreCards = true;
            FilteredCards.Clear();
            IsLoadingMoreCards = true;
            
            // Get the cancellation token for this operation
            var cancellationToken = _cts.Token;
            
            try
            {
                using (var context = new MnemoContext())
                {
                    // Apply search filter if any
                    var query = context.Decks
                        .Where(d => d.FlashcardId == SelectedDeck.Id);
                        
                    if (!string.IsNullOrWhiteSpace(CardSearchQuery))
                    {
                        // We need to load all cards first to apply client-side filtering
                        var allCards = await query.ToListAsync(cancellationToken);
                        
                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        // Apply the filter
                        var filtered = allCards.Where(c => 
                            c.Front.Contains(CardSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                            c.Back.Contains(CardSearchQuery, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                            
                        // Get the count and first page
                        int totalCount = filtered.Count;
                        var firstPage = filtered.Take(PAGE_SIZE).ToList();
                        
                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        // Update the UI
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            FilteredCards.Clear(); // Ensure we're starting fresh
                            
                            foreach (var card in firstPage)
                            {
                                FilteredCards.Add(card);
                            }
                            
                            // Update if we have more cards
                            _hasMoreCards = totalCount > PAGE_SIZE;
                            
                            // Update all cards to contain just the filtered results for pagination
                            _allDeckCards = filtered;
                            
                            // Notify that the collection has been updated
                            OnPropertyChanged(nameof(FilteredCards));
                        }, DispatcherPriority.Normal, cancellationToken);
                    }
                    else
                    {
                        // Get the first page and count
                        var firstPage = await query.Take(PAGE_SIZE).ToListAsync(cancellationToken);
                        
                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();
                            
                        var totalCount = await query.CountAsync(cancellationToken);
                        
                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        // Get all IDs for pagination
                        var allIds = await query.Select(d => d.Id).ToListAsync(cancellationToken);
                        
                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        // Update the UI
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            FilteredCards.Clear(); // Ensure we're starting fresh
                            
                            foreach (var card in firstPage)
                            {
                                FilteredCards.Add(card);
                            }
                            
                            // Update if we have more cards
                            _hasMoreCards = totalCount > PAGE_SIZE;
                            
                            // Update all cards to contain just the IDs for pagination
                            _allDeckCards = allIds.Select(id => new Deck { Id = id }).ToList();
                            
                            // Notify that the collection has been updated
                            OnPropertyChanged(nameof(FilteredCards));
                        }, DispatcherPriority.Normal, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, no need to do anything
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to filter cards: {ex.Message}");
            }
            finally
            {
                if (!_cts.IsCancellationRequested)
                {
                    IsLoadingMoreCards = false;
                }
            }
        }

        private async void LoadDeckCards()
        {
            if (SelectedDeck == null) return;

            try
            {
                // Reset lazy loading state
                _currentPage = 0;
                _hasMoreCards = true;
                _isLoadingMore = false;
                IsLoadingMoreCards = true;
                LoadingStatus = "Loading cards...";
                
                // Get the token for this operation
                var cancellationToken = _cts.Token;
                
                // Create a separate context for this operation to avoid concurrent access
                using (var context = new MnemoContext())
                {
                    // First just load the total card count to know if there are more cards
                    var totalCards = await context.Decks
                        .Where(d => d.FlashcardId == SelectedDeck.Id)
                        .CountAsync(cancellationToken);
                        
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                        
                    // Now load just the first page of cards
                    var firstBatch = await context.Decks
                        .Where(d => d.FlashcardId == SelectedDeck.Id)
                        .Take(PAGE_SIZE)
                        .ToListAsync(cancellationToken);
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Store all card IDs for pagination but don't load all content yet
                    var cardIds = await context.Decks
                        .Where(d => d.FlashcardId == SelectedDeck.Id)
                        .Select(d => d.Id)
                        .ToListAsync(cancellationToken);
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Update the UI with the first batch
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Initialize the all deck cards list to ensure we have IDs for paging
                        _allDeckCards = new List<Deck>();
                        foreach (var id in cardIds)
                        {
                            _allDeckCards.Add(new Deck { Id = id });
                        }
                        
                        // Update the selected deck's card count
                        SelectedDeck.CardCount = totalCards;
                        
                        // Clear and add the first batch of cards to the filtered collection
                        FilteredCards.Clear();
                        foreach (var card in firstBatch)
                        {
                            FilteredCards.Add(card);
                        }
                        
                        // Update if we have more cards
                        _hasMoreCards = totalCards > PAGE_SIZE;
                        
                        // Notify that the collection has been updated
                        OnPropertyChanged(nameof(FilteredCards));
                    }, DispatcherPriority.Normal, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, no need to do anything
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to load deck cards: {ex.Message}");
            }
            finally
            {
                if (!_cts.IsCancellationRequested)
                {
                    IsLoadingMoreCards = false;
                }
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

        private void LearnFlashcard(Flashcard flashcard)
        {
            if (flashcard != null)
            {
                _navigationService.NavigateTo(new FlashcardPracticeViewModel(flashcard, _navigationService));
            }
        }

        private void ExportFlashcard(Flashcard flashcard)
        {
            if (flashcard != null)
            {
                var exportOverlay = new Views.Overlays.ExportOverlay
                {
                    DataContext = new ViewModels.Overlays.ExportOverlayViewModel(_mainWindow, ExportType.FlashcardDeck, flashcard.Id)
                };

                OverlayService.Instance.ShowOverlay(exportOverlay);
            }
            else
            {
                NotificationService.Warning("Please select a deck to export first.");
            }
        }

        private void ImportFlashcard(Flashcard flashcard)
        {
            var importViewModel = new ViewModels.Overlays.ImportOverlayViewModel();
            importViewModel.ImportCompletedCallback = async () => await LoadFlashcardsAsync();

            var importOverlay = new Views.Overlays.ImportOverlay
            {
                DataContext = importViewModel
            };

            // Register for the overlay closed event
            EventHandler handler = null;
            handler = async (s, e) =>
            {
                // Unsubscribe to avoid memory leaks
                OverlayService.Instance.OverlayClosed -= handler;
                
                // Refresh the flashcards
                await LoadFlashcardsAsync();
            };
            
            OverlayService.Instance.OverlayClosed += handler;

            // Show the overlay and set the host window
            OverlayService.Instance.ShowOverlay(importOverlay);
            importViewModel.SetHostWindow(_mainWindow);
        }

        // Add OnScrollChanged method to detect when user scrolls near the bottom
        public void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Check if we're close to the bottom (within 100 pixels)
                bool nearBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - 100;
                
                if (nearBottom && _hasMoreCards && !_isLoadingMore)
                {
                    LoadMoreCards();
                }
            }
        }

        private async void LoadMoreCards()
        {
            if (_isLoadingMore || !_hasMoreCards || SelectedDeck == null) return;
            
            _isLoadingMore = true;
            IsLoadingMoreCards = true;
            LoadingStatus = "Loading more cards...";
            
            // Get the cancellation token for this operation
            var cancellationToken = _cts.Token;
            
            try
            {
                _currentPage++;
                int skipCount = _currentPage * PAGE_SIZE;
                
                using (var context = new MnemoContext())
                {
                    // Get the IDs of the cards we need to load in this batch
                    var cardIds = _allDeckCards
                        .Skip(skipCount)
                        .Take(PAGE_SIZE)
                        .Select(d => d.Id)
                        .ToList();
                    
                    // Skip loading if no IDs to load
                    if (cardIds.Count == 0)
                    {
                        _hasMoreCards = false;
                        return;
                    }
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Load the actual card data from the database for this batch
                    var cards = await context.Decks
                        .Where(d => cardIds.Contains(d.Id))
                        .ToListAsync(cancellationToken);
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // If we have search filters, apply them
                    if (!string.IsNullOrWhiteSpace(CardSearchQuery))
                    {
                        cards = cards.Where(c =>
                            c.Front.Contains(CardSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                            c.Back.Contains(CardSearchQuery, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Add cards to the observable collection on UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        foreach (var card in cards)
                        {
                            FilteredCards.Add(card);
                        }
                        
                        // If we got fewer cards than requested, we've reached the end
                        _hasMoreCards = cards.Count == PAGE_SIZE && skipCount + PAGE_SIZE < _allDeckCards.Count;
                        
                        // Notify that the collection has been updated
                        OnPropertyChanged(nameof(FilteredCards));
                    }, DispatcherPriority.Normal, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, no need to do anything
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to load more cards: {ex.Message}");
            }
            finally
            {
                if (!_cts.IsCancellationRequested)
                {
                    _isLoadingMore = false;
                    IsLoadingMoreCards = false;
                }
            }
        }

        // Helper method to cancel any ongoing card loading operations
        private void CancelOngoingOperations()
        {
            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
            }
            catch
            {
                // Ignore any cancellation errors
            }
            finally
            {
                _cts = new CancellationTokenSource();
            }
        }

        // Dispose method to clean up resources
        public void Dispose()
        {
            try
            {
                // Dispose of the cancellation token source
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
            }
            catch
            {
                // Ignore any errors during disposal
            }
        }
    }
}
