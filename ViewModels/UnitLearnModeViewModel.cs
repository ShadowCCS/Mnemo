using MnemoProject.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MnemoProject.ViewModels
{
    public partial class UnitLearnModeViewModel : ViewModelBase
    {
        private readonly AIService? _aiService;
        private readonly NavigationService _navigationService;
        private string _unitContent = string.Empty;
        public bool IsLoadingStatusError => !string.IsNullOrEmpty(LoadingStatus) && !LoadingStatus.StartsWith("Error");
        private System.Threading.CancellationTokenSource? _generationCts;
        private readonly object _stateLock = new object();
        private bool _isGenerating = false; // Add flag to prevent multiple generations

        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private string _loadingStatus = "Generating learn content...";
        
        [ObservableProperty]
        private int _currentIndex = 0;
        
        [ObservableProperty]
        private int _progressValue = 0;
        
        [ObservableProperty]
        private bool _isInQuestionMode = false;
        
        [ObservableProperty]
        private string _currentTheory = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInQuestionMode), nameof(ShowMultipleChoice), nameof(ShowFillInBlank), nameof(ShowFreeWrite))]
        private LearnQuestion _currentQuestion = new LearnQuestion();
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAnsweredQuestion), nameof(CanContinue))]
        private string _userAnswer = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAnsweredQuestion), nameof(CanContinue))]
        private string? _feedbackMessage = null;
        
        [ObservableProperty]
        private bool _isFeedbackCorrect = false;
        
        [ObservableProperty]
        private int _selectedMultipleChoiceIndex = -1;
        
        private List<LearnSection> _learnSections = new List<LearnSection>();
        
        public bool HasLearnContent => _learnSections.Count > 0;
        
        public bool CanGoNext => CurrentIndex < _learnSections.Count - 1 || (CurrentIndex < _learnSections.Count && IsInQuestionMode);
        
        public bool CanGoPrevious => CurrentIndex > 0 || IsInQuestionMode;
        
        public bool HasAnsweredQuestion => !string.IsNullOrEmpty(FeedbackMessage);
        
        public bool ShowMultipleChoice => CurrentQuestion?.Type == QuestionType.MultipleChoice;
        
        public bool ShowFillInBlank => CurrentQuestion?.Type == QuestionType.FillInBlank;
        
        public bool ShowFreeWrite => CurrentQuestion?.Type == QuestionType.FreeWrite;
        
        public bool CanContinue => !IsInQuestionMode || HasAnsweredQuestion;

        [ObservableProperty]
        private List<OptionItem> _optionItems = new List<OptionItem>();

        // Default constructor for design-time
        public UnitLearnModeViewModel()
        {
        }
        
        // Main constructor for runtime
        public UnitLearnModeViewModel(string unitContent, NavigationService navigationService)
        {
            _unitContent = unitContent;
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // Debug output to check unit content
            System.Diagnostics.Debug.WriteLine($"UnitLearnModeViewModel constructor called with content length: {unitContent?.Length ?? 0}");
            if (string.IsNullOrEmpty(unitContent))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Unit content is empty or null");
                _unitContent = "This is a sample unit content for generating learn mode since the actual content is empty.";
            }
            else
            {
                // Show first 100 chars of content
                System.Diagnostics.Debug.WriteLine($"Unit content starts with: {unitContent.Substring(0, Math.Min(100, unitContent.Length))}...");
            }
            
            var aiProvider = new GeminiProvider();
            var aiWorker = new AIWorker();
            _aiService = new AIService(aiProvider, aiWorker);
        }
        
        // Call this when the tab is activated
        public async Task OnActivated()
        {
            System.Diagnostics.Debug.WriteLine("UnitLearnModeViewModel activated");
            // Generate learn content if we don't have any yet and not already generating
            if (_learnSections.Count == 0 && !_isGenerating)
            {
                // Cancel any previous generation if it exists
                if (_generationCts != null)
                {
                    _generationCts.Cancel();
                    _generationCts.Dispose();
                    _generationCts = null;
                }
                
                _isGenerating = true;
                IsLoading = true;
                LoadingStatus = "Preparing to generate learn content...";
                try 
                {
                    await GenerateLearnContent();
                }
                finally
                {
                    _isGenerating = false;
                }
            }
        }
        
        [RelayCommand]
        private async Task GenerateLearnContent()
        {
            if (_learnSections.Count > 0 || _aiService == null)
                return;
                
            IsLoading = true;
            LoadingStatus = "Initializing AI service...";

            // Cancel any previous generation
            if (_generationCts != null)
            {
                _generationCts.Cancel();
                _generationCts.Dispose();
            }
            
            // Create a new cancellation token source
            _generationCts = new System.Threading.CancellationTokenSource();
            var cancellationToken = _generationCts.Token;

            System.Diagnostics.Debug.WriteLine($"Generating learn content for unit with content length: {_unitContent?.Length ?? 0}");
            
            if (string.IsNullOrWhiteSpace(_unitContent))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Unit content is empty, using placeholder");
                LoadingStatus = "Error: No content available to generate learning material";
                _unitContent = "This is sample content for learning.";
            }

            string prompt = @"Based on the following unit content, create a learning experience that alternates between theory sections and questions.
Break down the unit content into 3-5 logical sections. For each section:
1. Create a concise theory explanation (1-2 paragraphs)
2. Follow with 2-4 questions related to that theory

Include different types of questions:
- Multiple-choice (with exactly one correct answer)
- Fill-in-the-blank (with exact answer expected and 3-4 options to choose from)
- Free response (for checking conceptual understanding)

Format your response EXACTLY as a valid JSON array with the following structure:
[
  {
    ""theory"": ""Theory content for section 1..."",
    ""questions"": [
      {
        ""question"": ""Question text?"",
        ""type"": ""MultipleChoice"",
        ""options"": [""Option A"", ""Option B"", ""Option C"", ""Option D""],
        ""correctOptionIndex"": 0,
        ""explanation"": ""Why this answer is correct...""
      },
      {
        ""question"": ""What is the role of the _______ in the respiratory system?"",
        ""type"": ""FillInBlank"",
        ""options"": [""Alveoli"", ""Phalanx"", ""Capillaries""],
        ""correctAnswer"": ""Alveoli"",
        ""explanation"": ""Explanation for why this answer is correct""
      },
      {
        ""question"": ""Explain the concept of X in your own words."",
        ""type"": ""FreeWrite"",
        ""sampleAnswer"": ""A good answer would discuss..."",
        ""keywords"": [""important"", ""concept"", ""terms""]
      }
    ]
  },
  // Additional theory/question sections...
]

Use the content to create substantial, educational material that helps learners understand and apply key concepts.

Unit Content:
" + _unitContent;

            try
            {
                LoadingStatus = "Contacting AI service...";
                
                // Check if cancellation was requested
                if (cancellationToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("Content generation was cancelled");
                    return;
                }
                
                // We need to create a task that we can await
                var taskCompletionSource = new TaskCompletionSource<string>();
                
                // If cancellation is requested, cancel the task
                cancellationToken.Register(() => 
                {
                    taskCompletionSource.TrySetCanceled();
                });
                
                // Call the AI service to generate learn content
                await _aiService.GenerateComplexOutput(prompt, (response) => {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.TrySetResult(response);
                    }
                });
                
                // Wait for the AI service to complete
                string learnContentJson = await taskCompletionSource.Task;
                
                // Check for cancellation again
                if (cancellationToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("Content generation was cancelled after response received");
                    return;
                }
                
                // Process the learning content
                ProcessLearnContent(learnContentJson);
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Content generation task was cancelled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating learn content: {ex.Message}");
                LoadingStatus = $"Error: {ex.Message}";
                HandleGenerationError("Failed to generate learning content", ex);
            }
            finally
            {
                IsLoading = !HasLearnContent;
            }
        }
        
        private void ProcessLearnContent(string learnContentJson)
        {
            lock (_stateLock)
            {
                try
                {
                    // If we already have content, don't process more
                    if (_learnSections.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Content already processed, skipping...");
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Processing learn content JSON: {learnContentJson.Substring(0, Math.Min(100, learnContentJson.Length))}...");
                    
                    // Validate JSON before attempting to deserialize
                    if (string.IsNullOrWhiteSpace(learnContentJson) || !learnContentJson.Trim().StartsWith("["))
                    {
                        throw new InvalidOperationException("Invalid JSON format: response is not an array");
                    }
                    
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    };
                    
                    var generatedSections = JsonSerializer.Deserialize<List<LearnSection>>(learnContentJson, jsonOptions);
                    
                    if (generatedSections == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize learning content");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Deserialized {generatedSections.Count} learn sections");
                    
                    // Clean up any potentially problematic default options
                    foreach (var section in generatedSections)
                    {
                        if (section.Questions != null)
                        {
                            foreach (var question in section.Questions)
                            {
                                // Remove default options for FillInBlank questions
                                if (question.Type == QuestionType.FillInBlank)
                                {
                                    if (question.Options != null && question.Options.Any(o => o.StartsWith("Option ")))
                                    {
                                        var cleanOptions = question.Options.Where(o => !o.StartsWith("Option ")).ToList();
                                        question.Options = cleanOptions;
                                    }
                                }
                                // Clear options completely for FreeWrite questions
                                else if (question.Type == QuestionType.FreeWrite)
                                {
                                    question.Options = new List<string>();
                                }
                            }
                        }
                    }
                    
                    // Validate each section and its questions before storing
                    var validSections = new List<LearnSection>();
                    foreach (var section in generatedSections)
                    {
                        // Skip sections with empty theory
                        if (string.IsNullOrWhiteSpace(section.Theory))
                        {
                            continue;
                        }
                        
                        // Ensure Questions is not null
                        section.Questions ??= new List<LearnQuestion>();
                        
                        // Validate and fix each question
                        var validQuestions = new List<LearnQuestion>();
                        foreach (var question in section.Questions)
                        {
                            if (question != null && ValidateAndFixQuestion(question))
                            {
                                validQuestions.Add(question);
                            }
                        }
                        
                        // Only add sections with at least one valid question
                        if (validQuestions.Count > 0)
                        {
                            section.Questions = validQuestions;
                            validSections.Add(section);
                        }
                    }
                    
                    // Only proceed if we have valid sections
                    if (validSections.Count > 0)
                    {
                        // Store the validated learn sections
                        _learnSections = validSections;
                        
                        // Load the first section's theory
                        LoadTheorySection(0);
                        
                        // Hide loading screen
                        IsLoading = false;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No valid learning sections found after validation");
                        throw new InvalidOperationException("No valid learning sections found after validation");
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR parsing learn content JSON: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Path: {ex.Path}, LineNumber: {ex.LineNumber}, BytePositionInLine: {ex.BytePositionInLine}");
                    HandleGenerationError("Error parsing AI response", ex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR parsing learn content: {ex.Message}");
                    HandleGenerationError("Error processing learning content", ex);
                }
            }
        }
        
        // Validate and fix question data to prevent runtime errors
        private bool ValidateAndFixQuestion(LearnQuestion question)
        {
            // Skip questions with no text
            if (string.IsNullOrWhiteSpace(question.Question))
            {
                return false;
            }
            
            // Ensure explanation isn't null
            question.Explanation ??= "No explanation provided.";
            
            switch (question.Type)
            {
                case QuestionType.MultipleChoice:
                    // Initialize the Options list if null
                    question.Options ??= new List<string>();
                    
                    // Ensure options exist and aren't empty
                    if (question.Options.Count < 2)
                    {
                        return false; // Skip questions with insufficient options
                    }
                    
                    // Ensure correctOptionIndex is valid
                    if (question.CorrectOptionIndex < 0 || question.CorrectOptionIndex >= question.Options.Count)
                    {
                        return false; // Skip questions with invalid correct option index
                    }
                    break;
                
                case QuestionType.FillInBlank:
                    // For fill-in-blank, don't keep the default options
                    if (question.Options == null)
                    {
                        question.Options = new List<string>();
                    }
                    else if (question.Options.Contains("Option A"))
                    {
                        // Clear default options if they exist
                        var realOptions = question.Options.Where(o => !o.StartsWith("Option ")).ToList();
                        question.Options = realOptions;
                    }
                    
                    if (question.Options.Count < 2 || string.IsNullOrWhiteSpace(question.CorrectAnswer))
                    {
                        return false; // Skip questions with insufficient options or missing correct answer
                    }
                    
                    // Ensure correctAnswer is in the options
                    if (!question.Options.Contains(question.CorrectAnswer))
                    {
                        return false; // Skip questions where correct answer isn't in options
                    }
                    break;
                
                case QuestionType.FreeWrite:
                    // Clear any options for free write questions
                    question.Options = new List<string>();
                    
                    // Ensure sampleAnswer exists
                    if (string.IsNullOrWhiteSpace(question.SampleAnswer))
                    {
                        return false; // Skip questions without sample answer
                    }
                    
                    // Ensure keywords exist
                    question.Keywords ??= new List<string>();
                    if (question.Keywords.Count == 0)
                    {
                        return false; // Skip questions without keywords
                    }
                    break;
                
                default:
                    return false;
            }
            
            return true;
        }
        
        private void LoadTheorySection(int index)
        {
            lock (_stateLock)
            {
                try
                {
                    if (index >= 0 && index < _learnSections.Count)
                    {
                        CurrentIndex = index;
                        IsInQuestionMode = false;
                        CurrentTheory = _learnSections[index].Theory;
                        FeedbackMessage = null;
                        UserAnswer = string.Empty;
                        SelectedMultipleChoiceIndex = -1;
                        UpdateOptionItems();
                        UpdateProgress();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in LoadTheorySection: {ex.Message}");
                    NotificationService.Error("An error occurred while loading content. Please try again.");
                }
            }
        }
        
        private void UpdateOptionItems()
        {
            // Fix Issue #2: Don't populate option items for FreeWrite questions
            if (CurrentQuestion?.Type == QuestionType.FreeWrite)
            {
                // For free-write questions, don't populate options
                OptionItems = new List<OptionItem>();
                return;
            }
            
            // For multiple choice and fill-in-blank, populate options normally
            if (CurrentQuestion?.Options != null)
            {
                OptionItems = CurrentQuestion.Options
                    .Select((text, index) => new OptionItem(text, index))
                    .ToList();
            }
            else
            {
                OptionItems = new List<OptionItem>();
            }
        }
        
        private void LoadQuestionSection(int sectionIndex, int questionIndex)
        {
            lock (_stateLock)
            {
                try
                {
                    // Validate indices first to prevent out of range exceptions
                    if (sectionIndex < 0 || sectionIndex >= _learnSections.Count)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid section index: {sectionIndex}");
                        return;
                    }
                    
                    var section = _learnSections[sectionIndex];
                    if (section.Questions == null || section.Questions.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"No questions available for section {sectionIndex}");
                        return;
                    }
                    
                    if (questionIndex < 0 || questionIndex >= section.Questions.Count)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid question index: {questionIndex} for section {sectionIndex}");
                        return;
                    }
                    
                    CurrentIndex = sectionIndex;
                    IsInQuestionMode = true;
                    
                    // Reset question-related state before loading new question
                    FeedbackMessage = null;
                    UserAnswer = string.Empty;
                    SelectedMultipleChoiceIndex = -1;
                    
                    // Set the current question which will update UI visibility properties
                    CurrentQuestion = section.Questions[questionIndex];
                    
                    // Ensure question type has proper options set up
                    if (CurrentQuestion.Type == QuestionType.FreeWrite)
                    {
                        // For free write questions, ensure there are no options displayed
                        if (CurrentQuestion.Options?.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Warning: Free write question had options, clearing them");
                            CurrentQuestion.Options.Clear();
                        }
                    }
                    
                    // Update option items for multiple choice or fill-in-blank
                    UpdateOptionItems();
                    UpdateProgress();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in LoadQuestionSection: {ex.Message}");
                    NotificationService.Error("An error occurred while loading the question. Please try again.");
                }
            }
        }
        
        private void UpdateProgress()
        {
            try
            {
                if (_learnSections.Count > 0)
                {
                    // Calculate progress considering both theory and question sections
                    int totalSections = _learnSections.Count;
                    int questionsPerSection = Math.Max(1, (int)_learnSections.Average(s => s.Questions?.Count ?? 0));
                    int totalItems = totalSections * (1 + questionsPerSection); // Theories + Questions
                    
                    int currentItem = CurrentIndex * (1 + questionsPerSection);
                    if (IsInQuestionMode && CurrentQuestion != null)
                    {
                        // Find current question index - handle case where CurrentQuestion isn't in the collection
                        int questionIndex = -1;
                        if (CurrentIndex >= 0 && CurrentIndex < _learnSections.Count && 
                            _learnSections[CurrentIndex].Questions != null)
                        {
                            questionIndex = _learnSections[CurrentIndex].Questions.IndexOf(CurrentQuestion);
                        }
                        
                        if (questionIndex != -1) // Only add if found
                        {
                            currentItem += 1 + questionIndex; // +1 for the theory
                        }
                    }
                    
                    ProgressValue = (int)((currentItem / (double)totalItems) * 100);
                }
                else
                {
                    ProgressValue = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateProgress: {ex.Message}");
                ProgressValue = 0; // Set a default value on error
            }
        }
        
        [RelayCommand]
        public void Continue()
        {
            lock (_stateLock)
            {
                try
                {
                    // If we don't have any content or the index is invalid, do nothing
                    if (_learnSections.Count == 0 || CurrentIndex < 0 || CurrentIndex >= _learnSections.Count)
                    {
                        System.Diagnostics.Debug.WriteLine("Cannot continue: invalid state");
                        return;
                    }
                
                    // If we're in theory mode, switch to the first question for this section
                    if (!IsInQuestionMode)
                    {
                        if (_learnSections[CurrentIndex].Questions?.Count > 0)
                        {
                            LoadQuestionSection(CurrentIndex, 0);
                        }
                        else
                        {
                            // No questions for this theory, go to the next theory
                            NextSection();
                        }
                    }
                    else
                    {
                        // If we're in question mode and have answered the question, move to the next question or theory
                        if (HasAnsweredQuestion)
                        {
                            // Safety check
                            if (CurrentQuestion == null || _learnSections[CurrentIndex].Questions == null)
                            {
                                System.Diagnostics.Debug.WriteLine("Cannot continue: question is null");
                                return;
                            }
                        
                            int currentQuestionIndex = _learnSections[CurrentIndex].Questions.IndexOf(CurrentQuestion);
                            
                            // If there's another question in this section
                            if (currentQuestionIndex != -1 && currentQuestionIndex < _learnSections[CurrentIndex].Questions.Count - 1)
                            {
                                LoadQuestionSection(CurrentIndex, currentQuestionIndex + 1);
                            }
                            else
                            {
                                // Go to the next theory section
                                NextSection();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in Continue method: {ex.Message}");
                    NotificationService.Error("An error occurred while navigating. Please try again.");
                }
            }
        }
        
        private void NextSection()
        {
            if (CurrentIndex < _learnSections.Count - 1)
            {
                LoadTheorySection(CurrentIndex + 1);
            }
        }
        
        [RelayCommand]
        public void Previous()
        {
            lock (_stateLock)
            {
                try
                {
                    // If we don't have any content or the index is invalid, do nothing
                    if (_learnSections.Count == 0 || CurrentIndex < 0 || CurrentIndex >= _learnSections.Count)
                    {
                        System.Diagnostics.Debug.WriteLine("Cannot go previous: invalid state");
                        return;
                    }
                
                    if (IsInQuestionMode)
                    {
                        // Safety check for null questions
                        if (CurrentQuestion == null || _learnSections[CurrentIndex].Questions == null)
                        {
                            LoadTheorySection(CurrentIndex);
                            return;
                        }
                        
                        // If we're on the first question, go back to the theory
                        int currentQuestionIndex = _learnSections[CurrentIndex].Questions.IndexOf(CurrentQuestion);
                        if (currentQuestionIndex <= 0 || currentQuestionIndex == -1) // Handle not found case
                        {
                            LoadTheorySection(CurrentIndex);
                        }
                        else
                        {
                            // Go to the previous question
                            LoadQuestionSection(CurrentIndex, currentQuestionIndex - 1);
                        }
                    }
                    else
                    {
                        // Go to the previous theory (if not on the first one)
                        if (CurrentIndex > 0)
                        {
                            LoadTheorySection(CurrentIndex - 1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in Previous method: {ex.Message}");
                    NotificationService.Error("An error occurred while navigating. Please try again.");
                }
            }
        }
        
        [RelayCommand]
        public void CheckAnswer()
        {
            if (IsInQuestionMode && !HasAnsweredQuestion)
            {
                try
                {
                    switch (CurrentQuestion.Type)
                    {
                        case QuestionType.MultipleChoice:
                            CheckMultipleChoiceAnswer();
                            break;
                        case QuestionType.FillInBlank:
                            CheckFillInBlankAnswer();
                            break;
                        case QuestionType.FreeWrite:
                            CheckFreeWriteAnswer();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking answer: {ex.Message}");
                    NotificationService.Error("An error occurred while checking your answer. Please try again.");
                    FeedbackMessage = "Sorry, there was an error checking your answer.";
                    IsFeedbackCorrect = false;
                }
            }
        }
        
        private void CheckMultipleChoiceAnswer()
        {
            if (SelectedMultipleChoiceIndex >= 0)
            {
                // Validate question data before proceeding
                if (CurrentQuestion.Options == null || CurrentQuestion.Options.Count == 0)
                {
                    throw new InvalidOperationException("Question options are missing");
                }
                
                if (CurrentQuestion.CorrectOptionIndex < 0 || CurrentQuestion.CorrectOptionIndex >= CurrentQuestion.Options.Count)
                {
                    throw new InvalidOperationException("Invalid correct option index");
                }
                
                bool isCorrect = SelectedMultipleChoiceIndex == CurrentQuestion.CorrectOptionIndex;
                IsFeedbackCorrect = isCorrect;
                
                if (isCorrect)
                {
                    FeedbackMessage = $"Correct! {CurrentQuestion.Explanation}";
                }
                else
                {
                    string correctAnswer = CurrentQuestion.Options[CurrentQuestion.CorrectOptionIndex];
                    FeedbackMessage = $"Incorrect. The correct answer is: {correctAnswer}. {CurrentQuestion.Explanation}";
                }
            }
            else
            {
                FeedbackMessage = "Please select an answer before checking.";
                IsFeedbackCorrect = false;
            }
        }
        
        private void CheckFillInBlankAnswer()
        {
            // Fix Issue #1: Don't show error when user has selected an answer button
            // We need to check if UserAnswer is actually empty, not just whitespace
            if (string.IsNullOrEmpty(UserAnswer))
            {
                FeedbackMessage = "Please select an answer before checking.";
                IsFeedbackCorrect = false;
                return;
            }
            
            // Validate question data
            if (string.IsNullOrWhiteSpace(CurrentQuestion.CorrectAnswer))
            {
                throw new InvalidOperationException("Correct answer is missing");
            }
            
            // Compare answers ignoring case and whitespace
            bool isCorrect = string.Equals(
                UserAnswer.Trim(), 
                CurrentQuestion.CorrectAnswer.Trim(), 
                StringComparison.OrdinalIgnoreCase
            );
            
            IsFeedbackCorrect = isCorrect;
            
            if (isCorrect)
            {
                FeedbackMessage = $"Correct! {CurrentQuestion.Explanation}";
            }
            else
            {
                FeedbackMessage = $"Incorrect. The correct answer is: {CurrentQuestion.CorrectAnswer}. {CurrentQuestion.Explanation}";
            }
        }
        
        private void CheckFreeWriteAnswer()
        {
            if (string.IsNullOrWhiteSpace(UserAnswer))
            {
                FeedbackMessage = "Please enter an answer before checking.";
                IsFeedbackCorrect = false;
                return;
            }
            
            // Validate question data
            if (string.IsNullOrWhiteSpace(CurrentQuestion.SampleAnswer))
            {
                CurrentQuestion.SampleAnswer = "No sample answer provided.";
            }
            
            if (CurrentQuestion.Keywords == null || CurrentQuestion.Keywords.Count == 0)
            {
                CurrentQuestion.Keywords = new List<string> { "key", "concept" };
            }
            
            // For free-write, do a simple keyword check rather than sending to AI
            // This minimizes token usage while still providing feedback
            bool containsKeywords = CurrentQuestion.Keywords.Any(k => 
                UserAnswer.ToLower().Contains(k.ToLower()));
            
            // Always provide encouraging feedback for free-write responses
            IsFeedbackCorrect = true;
            
            if (containsKeywords)
            {
                FeedbackMessage = $"Great job! Your answer includes key concepts. A model answer might be: {CurrentQuestion.SampleAnswer}";
            }
            else
            {
                FeedbackMessage = $"Thank you for your response. A model answer might be: {CurrentQuestion.SampleAnswer}";
            }
            
            // Note: If AI-based checking is required in the future, 
            // this would be the place to add a simple API call with minimal prompt
        }
        
        [RelayCommand]
        public void SkipQuestion()
        {
            if (IsInQuestionMode)
            {
                try
                {
                    // Set feedback to indicate skipping
                    IsFeedbackCorrect = false;
                    FeedbackMessage = "Question skipped. ";
                    
                    if (CurrentQuestion.Type == QuestionType.MultipleChoice && 
                        CurrentQuestion.Options != null && 
                        CurrentQuestion.CorrectOptionIndex >= 0 && 
                        CurrentQuestion.CorrectOptionIndex < CurrentQuestion.Options.Count)
                    {
                        string correctAnswer = CurrentQuestion.Options[CurrentQuestion.CorrectOptionIndex];
                        FeedbackMessage += $"The correct answer was: {correctAnswer}. {CurrentQuestion.Explanation}";
                    }
                    else if (CurrentQuestion.Type == QuestionType.FillInBlank && 
                             !string.IsNullOrWhiteSpace(CurrentQuestion.CorrectAnswer))
                    {
                        FeedbackMessage += $"The correct answer was: {CurrentQuestion.CorrectAnswer}. {CurrentQuestion.Explanation}";
                    }
                    else if (CurrentQuestion.Type == QuestionType.FreeWrite && 
                             !string.IsNullOrWhiteSpace(CurrentQuestion.SampleAnswer))
                    {
                        FeedbackMessage += $"A model answer would be: {CurrentQuestion.SampleAnswer}";
                    }
                    else
                    {
                        FeedbackMessage += "No answer information available.";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in SkipQuestion: {ex.Message}");
                    FeedbackMessage = "Question skipped.";
                }
            }
        }
        
        // Handles navigation back to the unit guide when content cannot be generated
        [RelayCommand]
        public void NavigateBackToUnitGuide()
        {
            if (_navigationService != null)
            {
                _navigationService.GoBack();
                NotificationService.Warning("Returned to Unit Guide due to content generation issues.");
            }
        }
        
        private void HandleGenerationError(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{message}: {ex.Message}");
            LoadingStatus = $"Error: {message}";
            IsLoading = true;
            
            // Show notification using static method
            NotificationService.Error($"{message}. {ex.Message}", "AI Generation Failed");
        }
        
        [RelayCommand]
        public void SelectMultipleChoiceOption(object param)
        {
            if (param is int index)
            {
                SelectedMultipleChoiceIndex = index;
            }
            else if (param is string indexStr && int.TryParse(indexStr, out int parsedIndex))
            {
                SelectedMultipleChoiceIndex = parsedIndex;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Invalid parameter for SelectMultipleChoiceOption: {param}");
            }
        }
        
        [RelayCommand]
        public void SelectAnswer(string answer)
        {
            UserAnswer = answer;
        }
    }

    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum QuestionType
    {
        MultipleChoice,
        FillInBlank,
        FreeWrite
    }

    public class LearnSection
    {
        public string Theory { get; set; } = string.Empty;
        public List<LearnQuestion> Questions { get; set; } = new List<LearnQuestion>();
    }

    public class LearnQuestion
    {
        public string Question { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
        
        // For multiple choice
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOptionIndex { get; set; }
        
        // For fill in blank
        public string? CorrectAnswer { get; set; }
        
        // For free write
        public string? SampleAnswer { get; set; }
        public List<string>? Keywords { get; set; }
        
        // For all question types
        public string? Explanation { get; set; }
    }

    public class OptionItem
    {
        public string Text { get; set; }
        public int Index { get; set; }

        public OptionItem(string text, int index)
        {
            Text = text;
            Index = index;
        }
    }

    public class IndexToBoolConverter : IValueConverter
    {
        public static IndexToBoolConverter Instance { get; } = new IndexToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index && parameter is int selectedIndex)
            {
                return index == selectedIndex;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 