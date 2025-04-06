using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public class UnitQuestionsViewModel : ViewModelBase
    {
        private readonly AIService? _aiService;
        private string _unitContent = string.Empty;
        private bool _isLoading = true;
        private string _loadingStatus = "Preparing your questions...";
        private string _currentQuestion = "";
        private List<string> _answerOptions = new List<string>() { "", "", "", "" };
        private int _correctAnswerIndex = -1;
        private int? _selectedAnswerIndex = null;
        private bool _hasSubmittedAnswer = false;
        private int _currentQuestionIndex = 0;
        private List<QuestionData> _questions = new List<QuestionData>();
        private double _progressValue = 0;
        private bool _isGeneratingMore = false;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string LoadingStatus
        {
            get => _loadingStatus;
            set => SetProperty(ref _loadingStatus, value);
        }

        public string CurrentQuestion
        {
            get => _currentQuestion;
            set => SetProperty(ref _currentQuestion, value);
        }

        public List<string> AnswerOptions
        {
            get => _answerOptions;
            set => SetProperty(ref _answerOptions, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public bool IsGeneratingMore
        {
            get => _isGeneratingMore;
            set => SetProperty(ref _isGeneratingMore, value);
        }

        public bool HasSelectedAnswer => _selectedAnswerIndex.HasValue;

        public bool HasSubmittedAnswer => _hasSubmittedAnswer;

        public bool CanGoPrevious => _currentQuestionIndex > 0;
        
        public bool CanGoNext => _currentQuestionIndex < _questions.Count - 1;

        // Background colors for options
        public IBrush Option1Background => GetOptionBackground(0);
        public IBrush Option2Background => GetOptionBackground(1);
        public IBrush Option3Background => GetOptionBackground(2);
        public IBrush Option4Background => GetOptionBackground(3);

        // Border colors for options
        public IBrush Option1BorderBrush => GetOptionBorderBrush(0);
        public IBrush Option2BorderBrush => GetOptionBorderBrush(1);
        public IBrush Option3BorderBrush => GetOptionBorderBrush(2);
        public IBrush Option4BorderBrush => GetOptionBorderBrush(3);

        // Commands
        public ICommand SelectAnswerCommand { get; }
        public ICommand SubmitAnswerCommand { get; }
        public ICommand TryAgainCommand { get; }
        public ICommand PreviousQuestionCommand { get; }
        public ICommand NextQuestionCommand { get; }
        public ICommand GenerateMoreQuestionsCommand { get; }

        public UnitQuestionsViewModel(string unitContent)
        {
            _unitContent = unitContent;
            
            // Debug output to check unit content
            System.Diagnostics.Debug.WriteLine($"UnitQuestionsViewModel constructor called with content length: {unitContent?.Length ?? 0}");
            if (string.IsNullOrEmpty(unitContent))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Unit content is empty or null");
                _unitContent = "This is a sample unit content for generating questions since the actual content is empty.";
            }
            else
            {
                // Show first 100 chars of content
                System.Diagnostics.Debug.WriteLine($"Unit content starts with: {unitContent.Substring(0, Math.Min(100, unitContent.Length))}...");
            }
            
            var aiProvider = new GeminiProvider();
            var aiWorker = new AIWorker();
            _aiService = new AIService(aiProvider, aiWorker);

            // Initialize commands
            SelectAnswerCommand = new RelayCommand<string>(SelectAnswer);
            SubmitAnswerCommand = new RelayCommand(SubmitAnswer, () => HasSelectedAnswer && !HasSubmittedAnswer);
            TryAgainCommand = new RelayCommand(TryAgain, () => HasSubmittedAnswer);
            PreviousQuestionCommand = new RelayCommand(PreviousQuestion, () => CanGoPrevious);
            NextQuestionCommand = new RelayCommand(NextQuestion, () => CanGoNext);
            GenerateMoreQuestionsCommand = new RelayCommand(GenerateMoreQuestions, () => !IsGeneratingMore);
        }

        // Call this when the tab is activated
        public void OnActivated()
        {
            System.Diagnostics.Debug.WriteLine("UnitQuestionsViewModel activated");
            // Generate questions if we don't have any yet
            if (_questions.Count == 0)
            {
                GenerateQuestions();
            }
        }

        private async void GenerateQuestions()
        {
            IsLoading = true;
            LoadingStatus = "Generating questions...";

            if (_aiService != null)
            {
                await _aiService.GenerateMultipleChoiceQuestions(_unitContent, OnQuestionsGenerated);
            }
            else
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("ERROR: AI Service is null");
            }
        }

        private void OnQuestionsGenerated(string questionsJson)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Received questions JSON: {questionsJson}");
                
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                
                var generatedQuestions = System.Text.Json.JsonSerializer.Deserialize<List<QuestionData>>(questionsJson, jsonOptions);
                
                if (generatedQuestions != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Deserialized {generatedQuestions.Count} questions");
                    foreach (var question in generatedQuestions.Take(3)) // Show just the first 3 for brevity
                    {
                        System.Diagnostics.Debug.WriteLine($"Deserialized question: {question.Question}");
                    }
                }
                
                if (generatedQuestions != null && generatedQuestions.Count > 0)
                {
                    // Use the new list of questions
                    _questions = new List<QuestionData>(generatedQuestions);
                    
                    // Reset to the first question
                    _currentQuestionIndex = 0;
                    
                    // Load the first question
                    LoadQuestion(_currentQuestionIndex);
                    
                    // Update navigation button states
                    UpdateNavigationCommands();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No questions were deserialized");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing questions: {ex.Message}");
            }
            
            IsLoading = false;
        }

        private void LoadQuestion(int index)
        {
            if (index >= 0 && index < _questions.Count)
            {
                _currentQuestionIndex = index;
                var question = _questions[index];
                
                CurrentQuestion = question.Question;
                AnswerOptions = question.Options;
                _correctAnswerIndex = question.CorrectOptionIndex;
                
                // Reset selection state
                _selectedAnswerIndex = null;
                _hasSubmittedAnswer = false;
                
                // Update properties to reflect new state
                OnPropertyChanged(nameof(HasSelectedAnswer));
                OnPropertyChanged(nameof(HasSubmittedAnswer));
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(Option1Background));
                OnPropertyChanged(nameof(Option2Background));
                OnPropertyChanged(nameof(Option3Background));
                OnPropertyChanged(nameof(Option4Background));
                OnPropertyChanged(nameof(Option1BorderBrush));
                OnPropertyChanged(nameof(Option2BorderBrush));
                OnPropertyChanged(nameof(Option3BorderBrush));
                OnPropertyChanged(nameof(Option4BorderBrush));
                
                UpdateProgress();
                
                // Update navigation button states
                UpdateNavigationCommands();
            }
        }

        private void SelectAnswer(string parameter)
        {
            if (!_hasSubmittedAnswer && int.TryParse(parameter, out int selectedIndex))
            {
                _selectedAnswerIndex = selectedIndex;
                OnPropertyChanged(nameof(HasSelectedAnswer));
                OnPropertyChanged(nameof(Option1Background));
                OnPropertyChanged(nameof(Option2Background));
                OnPropertyChanged(nameof(Option3Background));
                OnPropertyChanged(nameof(Option4Background));
                OnPropertyChanged(nameof(Option1BorderBrush));
                OnPropertyChanged(nameof(Option2BorderBrush));
                OnPropertyChanged(nameof(Option3BorderBrush));
                OnPropertyChanged(nameof(Option4BorderBrush));
                ((RelayCommand)SubmitAnswerCommand).RaiseCanExecuteChanged();
            }
        }

        private void SubmitAnswer()
        {
            if (_selectedAnswerIndex.HasValue)
            {
                _hasSubmittedAnswer = true;
                OnPropertyChanged(nameof(HasSubmittedAnswer));
                OnPropertyChanged(nameof(Option1Background));
                OnPropertyChanged(nameof(Option2Background));
                OnPropertyChanged(nameof(Option3Background));
                OnPropertyChanged(nameof(Option4Background));
                OnPropertyChanged(nameof(Option1BorderBrush));
                OnPropertyChanged(nameof(Option2BorderBrush));
                OnPropertyChanged(nameof(Option3BorderBrush));
                OnPropertyChanged(nameof(Option4BorderBrush));
                ((RelayCommand)SubmitAnswerCommand).RaiseCanExecuteChanged();
                ((RelayCommand)TryAgainCommand).RaiseCanExecuteChanged();
            }
        }

        private void TryAgain()
        {
            // Reset the selection state
            _selectedAnswerIndex = null;
            _hasSubmittedAnswer = false;
            
            // Update the UI
            OnPropertyChanged(nameof(HasSelectedAnswer));
            OnPropertyChanged(nameof(HasSubmittedAnswer));
            OnPropertyChanged(nameof(Option1Background));
            OnPropertyChanged(nameof(Option2Background));
            OnPropertyChanged(nameof(Option3Background));
            OnPropertyChanged(nameof(Option4Background));
            OnPropertyChanged(nameof(Option1BorderBrush));
            OnPropertyChanged(nameof(Option2BorderBrush));
            OnPropertyChanged(nameof(Option3BorderBrush));
            OnPropertyChanged(nameof(Option4BorderBrush));
            
            // Update command states
            ((RelayCommand)SubmitAnswerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)TryAgainCommand).RaiseCanExecuteChanged();
        }

        private void PreviousQuestion()
        {
            if (CanGoPrevious)
            {
                LoadQuestion(_currentQuestionIndex - 1);
            }
        }

        private void NextQuestion()
        {
            if (CanGoNext)
            {
                LoadQuestion(_currentQuestionIndex + 1);
            }
        }

        private async void GenerateMoreQuestions()
        {
            IsGeneratingMore = true;
            
            if (_aiService != null)
            {
                await _aiService.GenerateMultipleChoiceQuestions(_unitContent, OnMoreQuestionsGenerated);
            }
            else
            {
                IsGeneratingMore = false;
                System.Diagnostics.Debug.WriteLine("ERROR: AI Service is null");
            }
        }
        
        private void OnMoreQuestionsGenerated(string questionsJson)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Received additional questions JSON: {questionsJson}");
                
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                
                var generatedQuestions = System.Text.Json.JsonSerializer.Deserialize<List<QuestionData>>(questionsJson, jsonOptions);
                
                if (generatedQuestions != null && generatedQuestions.Count > 0)
                {
                    // Create a set of existing questions to avoid duplicates
                    HashSet<string> existingQuestions = new HashSet<string>(_questions.Select(q => q.Question));
                    
                    // Add only non-duplicate questions
                    int addedCount = 0;
                    foreach (var question in generatedQuestions)
                    {
                        if (!existingQuestions.Contains(question.Question))
                        {
                            _questions.Add(question);
                            existingQuestions.Add(question.Question);
                            addedCount++;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Added {addedCount} new questions");
                    
                    // Update progress
                    UpdateProgress();
                    
                    // Explicitly notify that CanGoNext might have changed
                    OnPropertyChanged(nameof(CanGoNext));
                    
                    // Update navigation button states
                    UpdateNavigationCommands();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing additional questions: {ex.Message}");
            }
            
            IsGeneratingMore = false;
        }

        private IBrush GetOptionBackground(int optionIndex)
        {
            if (!_hasSubmittedAnswer)
            {
                // Before submission, just highlight selected
                return _selectedAnswerIndex == optionIndex ? 
                    new SolidColorBrush(Color.Parse("#2d4a61")) : 
                    new SolidColorBrush(Color.Parse("#363636"));
            }
            else
            {
                // After submission, show correct/incorrect
                if (optionIndex == _correctAnswerIndex)
                {
                    // Correct answer
                    return new SolidColorBrush(Color.Parse("#1e5c2d"));
                }
                else if (optionIndex == _selectedAnswerIndex)
                {
                    // Selected wrong answer
                    return new SolidColorBrush(Color.Parse("#6b2424"));
                }
                else
                {
                    // Non-selected, non-correct
                    return new SolidColorBrush(Color.Parse("#363636"));
                }
            }
        }

        private IBrush GetOptionBorderBrush(int optionIndex)
        {
            if (!_hasSubmittedAnswer)
            {
                // Before submission
                return _selectedAnswerIndex == optionIndex ? 
                    new SolidColorBrush(Color.Parse("#2880b1")) : 
                    new SolidColorBrush(Color.Parse("#3a3a3a"));
            }
            else
            {
                // After submission
                if (optionIndex == _correctAnswerIndex)
                {
                    // Correct answer
                    return new SolidColorBrush(Color.Parse("#2a7d3d"));
                }
                else if (optionIndex == _selectedAnswerIndex)
                {
                    // Selected wrong answer
                    return new SolidColorBrush(Color.Parse("#8f3232"));
                }
                else
                {
                    // Non-selected, non-correct
                    return new SolidColorBrush(Color.Parse("#3a3a3a"));
                }
            }
        }

        private void UpdateProgress()
        {
            if (_questions.Count > 0)
            {
                ProgressValue = ((_currentQuestionIndex + 1) * 100.0) / _questions.Count;
            }
            else
            {
                ProgressValue = 0;
            }
        }

        // New method to update command states
        private void UpdateNavigationCommands()
        {
            ((RelayCommand)PreviousQuestionCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NextQuestionCommand).RaiseCanExecuteChanged();
        }
    }

    public class QuestionData
    {
        public string Question { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOptionIndex { get; set; }
    }
} 