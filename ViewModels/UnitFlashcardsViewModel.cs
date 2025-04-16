using MnemoProject.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using MnemoProject.Models;

namespace MnemoProject.ViewModels
{
    public partial class UnitFlashcardsViewModel : ViewModelBase
    {
        private readonly AIService? _aiService;
        private string _unitContent = string.Empty;
        
        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private string _loadingStatus = "Generating flashcards...";
        
        [ObservableProperty]
        private int _currentIndex = 0;
        
        [ObservableProperty]
        private bool _isFlipped = false;
        
        [ObservableProperty]
        private int _progressValue = 0;
        
        [ObservableProperty]
        private bool _isGeneratingMore = false;
        
        // Use a collection of temporary FlashcardItem objects instead of database entities
        private List<FlashcardItem> _flashcards = new List<FlashcardItem>();
        
        public string CurrentQuestion
        {
            get
            {
                string result = _flashcards.Count > 0 && CurrentIndex < _flashcards.Count 
                    ? _flashcards[CurrentIndex].Question 
                    : "No flashcards available";
                System.Diagnostics.Debug.WriteLine($"CurrentQuestion accessed, returning: {result}");
                return result;
            }
        }
            
        public string CurrentAnswer
        {
            get
            {
                string result = _flashcards.Count > 0 && CurrentIndex < _flashcards.Count 
                    ? _flashcards[CurrentIndex].Answer ?? "No answer available"
                    : "No answer available";
                System.Diagnostics.Debug.WriteLine($"CurrentAnswer accessed, returning: {result}");
                return result;
            }
        }
            
        public bool CanGoNext => _flashcards.Count > 0 && CurrentIndex < _flashcards.Count - 1;
        
        public bool CanGoPrevious => _flashcards.Count > 0 && CurrentIndex > 0;

        // Simple non-entity class for flashcards
        private class FlashcardItem
        {
            public string Question { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
        }

        public UnitFlashcardsViewModel(string unitContent)
        {
            _unitContent = unitContent;
            
            // Debug output to check unit content
            System.Diagnostics.Debug.WriteLine($"UnitFlashcardsViewModel constructor called with content length: {unitContent?.Length ?? 0}");
            if (string.IsNullOrEmpty(unitContent))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Unit content is empty or null");
                _unitContent = "This is a sample unit content for generating flashcards since the actual content is empty.";
            }
            else
            {
                // Show first 100 chars of content
                System.Diagnostics.Debug.WriteLine($"Unit content starts with: {unitContent.Substring(0, Math.Min(100, unitContent.Length))}...");
            }
            
            var aiProvider = new GeminiProvider();
            var aiWorker = new AIWorker();
            _aiService = new AIService(aiProvider, aiWorker);
            
            // Not generating flashcards immediately anymore - will be triggered when tab is selected
        }
        
        // Call this when the tab is activated
        public void OnActivated()
        {
            System.Diagnostics.Debug.WriteLine("UnitFlashcardsViewModel activated");
            // Generate flashcards if we don't have any yet
            if (_flashcards.Count == 0)
            {
                GenerateFlashcards();
            }
        }
        
        private async void GenerateFlashcards()
        {
            // Only generate flashcards if we don't have any yet
            if (_flashcards.Count > 0)
                return;
                
            IsLoading = true;
            LoadingStatus = "Generating flashcards...";
            
            if (_aiService != null)
            {
                await _aiService.GenerateFlashcards(_unitContent, OnFlashcardsGenerated);
            }
        }
        
        private void OnFlashcardsGenerated(string flashcardsJson)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Received flashcards JSON: {flashcardsJson}");
                
                // Debug the JSON directly
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                
                // Create a temporary class to deserialize the AI response
                var tempFlashcards = System.Text.Json.JsonSerializer.Deserialize<List<AIFlashcard>>(flashcardsJson, jsonOptions);
                
                // Check what was actually deserialized
                if (tempFlashcards != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Deserialized {tempFlashcards.Count} flashcards");
                    foreach (var card in tempFlashcards.Take(3)) // Show just the first 3 for brevity
                    {
                        System.Diagnostics.Debug.WriteLine($"Deserialized card: Q='{card.question}', A='{card.answer}'");
                    }
                }
                
                if (tempFlashcards != null && tempFlashcards.Count > 0)
                {
                    // Create a fresh list of new flashcard items (not entity objects)
                    var newFlashcards = new List<FlashcardItem>();
                    
                    for (int i = 0; i < tempFlashcards.Count; i++)
                    {
                        var card = tempFlashcards[i];
                        string question = card.question?.Trim() ?? string.Empty;
                        string answer = card.answer?.Trim() ?? string.Empty;
                        
                        System.Diagnostics.Debug.WriteLine($"Processing card {i+1}: Q='{question}', A='{answer}'");
                        
                        if (!string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(answer))
                        {
                            // Create a simple FlashcardItem instead of database entity
                            newFlashcards.Add(new FlashcardItem 
                            { 
                                Question = question, 
                                Answer = answer
                            });
                        }
                    }
                    
                    // Use the new list of flashcards if we have any
                    if (newFlashcards.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Created {newFlashcards.Count} new flashcards from AI response");
                        
                        // Completely replace the flashcards list instead of clearing and adding
                        _flashcards = new List<FlashcardItem>(newFlashcards);
                        
                        // Log each flashcard to verify content
                        for (int i = 0; i < Math.Min(3, _flashcards.Count); i++)
                        {
                            System.Diagnostics.Debug.WriteLine($"New AI flashcard {i+1}: Q='{_flashcards[i].Question}', A='{_flashcards[i].Answer}'");
                        }
                        
                        // Reset to the first card
                        CurrentIndex = 0;
                    }
                }
                
                UpdateCurrentCard();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing flashcards: {ex.Message}");
            }
            
            IsLoading = false;
        }
        
        private void UpdateCurrentCard()
        {
            System.Diagnostics.Debug.WriteLine($"UpdateCurrentCard called - Flashcards count: {_flashcards.Count}, CurrentIndex: {CurrentIndex}");
            
            // Log the current flashcard
            if (_flashcards.Count > 0 && CurrentIndex < _flashcards.Count)
            {
                var currentCard = _flashcards[CurrentIndex];
                System.Diagnostics.Debug.WriteLine($"Current flashcard - Q: '{currentCard.Question}' (Length: {currentCard.Question.Length})");
                System.Diagnostics.Debug.WriteLine($"Current flashcard - A: '{currentCard.Answer}' (Length: {currentCard.Answer.Length})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No current flashcard available");
            }
            
            OnPropertyChanged(nameof(CurrentQuestion));
            OnPropertyChanged(nameof(CurrentAnswer));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CanGoPrevious));
            
            // Calculate progress - ensure we're using correct calculation for percentage
            if (_flashcards.Count > 0)
            {
                ProgressValue = (int)(((CurrentIndex + 1) / (double)_flashcards.Count) * 100);
                System.Diagnostics.Debug.WriteLine($"Progress: {ProgressValue}% - Card {CurrentIndex + 1} of {_flashcards.Count}");
            }
            else
            {
                ProgressValue = 0;
                System.Diagnostics.Debug.WriteLine("Progress: 0% - No cards available");
            }
                
            IsFlipped = false;
        }
        
        [RelayCommand]
        public void FlipCard()
        {
            // Log before flip
            System.Diagnostics.Debug.WriteLine($"FlipCard called - Current state: isFlipped={IsFlipped}");
            
            // Toggle the state
            IsFlipped = !IsFlipped;
            
            // Log after flip
            System.Diagnostics.Debug.WriteLine($"After flip - New state: isFlipped={IsFlipped}");
            
            // These property changes need to be triggered to update the UI
            OnPropertyChanged(nameof(CurrentQuestion));
            OnPropertyChanged(nameof(CurrentAnswer));
        }
        
        [RelayCommand]
        public void NextCard()
        {
            if (CanGoNext)
            {
                CurrentIndex++;
                UpdateCurrentCard();
            }
        }
        
        [RelayCommand]
        public void PreviousCard()
        {
            if (CanGoPrevious)
            {
                CurrentIndex--;
                UpdateCurrentCard();
            }
        }
        
        [RelayCommand]
        public async Task GenerateMoreCards()
        {
            if (IsGeneratingMore || _aiService == null)
                return;
                
            IsGeneratingMore = true;
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Generating additional flashcards");
                
                // Create a list of existing questions for uniqueness check
                var existingQuestions = new HashSet<string>(
                    _flashcards.Select(f => f.Question.Trim().ToLowerInvariant())
                );
                
                string prompt = $@"Based on the unit content below, generate 5 MORE flashcards in a question and answer format.
These should be DIFFERENT from the following existing flashcards:

{string.Join("\n", _flashcards.Take(Math.Min(10, _flashcards.Count)).Select(f => $"- Q: {f.Question} A: {f.Answer}"))}

Make the new flashcards cover different aspects or details from the unit content that weren't addressed by the existing flashcards.
Format your response as a JSON array of flashcard objects with 'question' and 'answer' properties.

Unit Content:
{_unitContent}";

                // We need to create a task that we can await
                var taskCompletionSource = new TaskCompletionSource<string>();
                
                // Call the AI service to generate more flashcards
                await _aiService.GenerateFlashcards(prompt, (response) => {
                    taskCompletionSource.SetResult(response);
                });
                
                // Wait for the AI service to complete
                string flashcardsJson = await taskCompletionSource.Task;
                
                // Process the new flashcards
                ProcessAdditionalFlashcards(flashcardsJson, existingQuestions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating additional flashcards: {ex.Message}");
            }
            finally
            {
                IsGeneratingMore = false;
            }
        }
        
        private void ProcessAdditionalFlashcards(string flashcardsJson, HashSet<string> existingQuestions)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing additional flashcards JSON: {flashcardsJson}");
                
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                
                var additionalAIFlashcards = System.Text.Json.JsonSerializer.Deserialize<List<AIFlashcard>>(flashcardsJson, jsonOptions);
                
                if (additionalAIFlashcards != null && additionalAIFlashcards.Count > 0)
                {
                    // Create a fresh list for truly unique flashcards
                    var uniqueNewFlashcards = new List<FlashcardItem>();
                    
                    foreach (var card in additionalAIFlashcards)
                    {
                        string question = card.question?.Trim() ?? string.Empty;
                        string answer = card.answer?.Trim() ?? string.Empty;
                        
                        // Only add if both fields have content and question is unique
                        if (!string.IsNullOrWhiteSpace(question) && 
                            !string.IsNullOrWhiteSpace(answer) && 
                            !existingQuestions.Contains(question.ToLowerInvariant()))
                        {
                            uniqueNewFlashcards.Add(new FlashcardItem { Question = question, Answer = answer });
                            existingQuestions.Add(question.ToLowerInvariant()); // Add to prevent duplicates within new batch
                        }
                    }
                    
                    // If we found unique new flashcards, add them to our list
                    if (uniqueNewFlashcards.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Adding {uniqueNewFlashcards.Count} unique new flashcards");
                        
                        // Keep track of original count for progress calculation
                        int originalCount = _flashcards.Count;
                        
                        // Add the new cards to the existing list
                        _flashcards.AddRange(uniqueNewFlashcards);
                        
                        // Log some of the new cards
                        for (int i = 0; i < Math.Min(3, uniqueNewFlashcards.Count); i++)
                        {
                            var card = uniqueNewFlashcards[i];
                            System.Diagnostics.Debug.WriteLine($"New additional flashcard {i+1}: Q='{card.Question}', A='{card.Answer}'");
                        }
                        
                        // Update UI
                        OnPropertyChanged(nameof(CanGoNext));
                        OnPropertyChanged(nameof(CanGoPrevious));
                        UpdateProgressBar(); // Update the progress bar
                        
                        // Show notification using NotificationService
                        NotificationService.Success($"Added {uniqueNewFlashcards.Count} new flashcards!", "Flashcards Updated");
                        
                        // Notify user of new cards being added
                        System.Diagnostics.Debug.WriteLine($"Successfully added {uniqueNewFlashcards.Count} new flashcards (total now: {_flashcards.Count})");
                    }
                    else
                    {
                        NotificationService.Info("No new unique flashcards were found.", "Flashcards");
                        System.Diagnostics.Debug.WriteLine("No new unique flashcards were found in the AI response");
                    }
                }
                else
                {
                    NotificationService.Info("No additional flashcards were generated.", "Flashcards");
                    System.Diagnostics.Debug.WriteLine("No additional flashcards were generated");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error processing additional flashcards: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"Error processing additional flashcards: {ex.Message}");
            }
        }
        
        // Add a method to update the progress bar
        private void UpdateProgressBar()
        {
            if (_flashcards.Count > 0)
            {
                ProgressValue = (int)(((CurrentIndex + 1) / (double)_flashcards.Count) * 100);
                System.Diagnostics.Debug.WriteLine($"Progress updated: {ProgressValue}% - Card {CurrentIndex + 1} of {_flashcards.Count}");
            }
            else
            {
                ProgressValue = 0;
                System.Diagnostics.Debug.WriteLine("Progress updated: 0% - No cards available");
            }
        }
    }

    // Keep this class for deserialization
    public class AIFlashcard
    {
        public string question { get; set; } = string.Empty;
        public string answer { get; set; } = string.Empty;
    }
} 