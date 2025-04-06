using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using MnemoProject.Models;
using MnemoProject.Services;
using MnemoProject.Data;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace MnemoProject.ViewModels
{
    partial class LearningPathCreateViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private readonly AIService _aiService;
        private readonly DatabaseService _databaseService = new();

        public ObservableCollection<Unit> Units { get; } = new();

        [ObservableProperty]
        private string _userInput = "";

        [ObservableProperty]
        private string _aiResponse;

        [ObservableProperty]
        private string _genStatus = "Initializing...";

        // Regular properties for HasUnits and IsGenerationComplete
        private bool _hasUnits;
        public bool HasUnits
        {
            get => _hasUnits;
            set => SetProperty(ref _hasUnits, value);
        }
        
        private bool _isGenerationComplete;
        public bool IsGenerationComplete
        {
            get => _isGenerationComplete;
            set => SetProperty(ref _isGenerationComplete, value);
        }
        
        private Guid _learningPathId;

        public ICommand GoBackCommand { get; }

        public LearningPathCreateViewModel(NavigationService navigationService, string userInput)
        {
            _navigationService = navigationService;
            GoBackCommand = new RelayCommand(ExecuteGoBack);

            var aiProvider = new GeminiProvider();
            var aiWorker = new AIWorker();
            _aiService = new AIService(aiProvider, aiWorker);

            _userInput = userInput;

            GenStatus = "Analyzing request...";
            NotificationService.Info("Starting learning path generation...");

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await GenerateLearningPath();
        }


        private void ExecuteGoBack()
        {
            // If generation is complete, navigate to the unit overview
            if (IsGenerationComplete && _learningPathId != Guid.Empty)
            {
                ContinueToUnitOverview(_learningPathId);
            }
            else
            {
                // Otherwise go back to the previous screen
                _navigationService.GoBack();
            }
        }

        private bool _learningPathProcessed = false;

        public async Task GenerateLearningPath()
        {
            try
            {
                await _aiService.GenerateLearningPathOutline(UserInput, async content =>
                {
                    if (_learningPathProcessed) return;
                    _learningPathProcessed = true;

                    GenStatus = "Creating learning structure...";
                    
                    var learningPath = await SaveLearningPathToDatabase(content);
                    if (learningPath != null && learningPath.Units.Any())
                    {
                        _learningPathId = learningPath.Id;
                        System.Diagnostics.Debug.WriteLine("[GenerateLearningPath] Processing learningPath with units.");
                        NotificationService.Info($"Creating learning path: {learningPath.Title}");
                        
                        // Store the total number of units for progress display
                        int totalUnits = learningPath.Units.Count;
                        int processedUnits = 0;
                        
                        // Process each unit
                        foreach (var unit in learningPath.Units)
                        {
                            processedUnits++;
                            unit.LearningPathId = learningPath.Id;
                            unit.UnitNumber = processedUnits;
                            
                            if (unit.Id == Guid.Empty)
                            {
                                unit.Id = Guid.NewGuid();
                            }

                            // Add the unit to the collection with placeholder content
                            unit.UnitContent = "Generating content...";
                            Units.Add(unit);
                            HasUnits = true;

                            // Only generate content for the first unit (Unit 1)
                            if (processedUnits == 1)
                            {
                                GenStatus = $"Generating unit {processedUnits}/{totalUnits}: {unit.Title}";
                                NotificationService.Info($"Generating unit {processedUnits}/{totalUnits}: {unit.Title}");

                                // Prepare theory content
                                string theoryContent = !string.IsNullOrWhiteSpace(unit.TheoryContent)
                                    ? unit.TheoryContent
                                    : $"Generate content for '{unit.Title}'.";
                                
                                // Generate content and update the unit
                                await GenerateUnitContent(learningPath, unit, theoryContent);

                                // Save the updated content to the database
                                await SaveUnitToDatabase(unit);
                            }
                            else
                            {
                                // Just save the unit with placeholder content for now
                                unit.UnitContent = "Content will be generated when you open this unit.";
                                await SaveUnitToDatabase(unit);
                            }
                            
                            // Update UI after processing
                            GenStatus = $"Unit {processedUnits}/{totalUnits} processed";
                        }

                        GenStatus = "Learning path creation complete!";
                        NotificationService.Success("Learning path created successfully!");
                        IsGenerationComplete = true;
                        
                        // Automatically navigate to unit overview when complete
                        ContinueToUnitOverview(_learningPathId);
                    }
                    else
                    {
                        GenStatus = "Failed to create learning path";
                        NotificationService.Error("Failed to create learning path. Please try again.");
                        IsGenerationComplete = true;
                    }
                });
            }
            catch (Exception ex)
            {
                GenStatus = "Error occurred";
                NotificationService.Error($"Error generating learning path: {ex.Message}");
                NotificationService.LogToFile($"[ERROR] GenerateLearningPath: {ex}");
                IsGenerationComplete = true;
            }
        }

        public async Task GenerateUnitContent(LearningPath learningPath, Unit unit, string theoryContent)
        {
            System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] Starting for unit: {unit.Title}");
            NotificationService.LogToFile($"Generating content for unit: {unit.Title}");

            try
            {
                if (string.IsNullOrWhiteSpace(theoryContent))
                {
                    theoryContent = $"Generate detailed content for the unit titled '{unit.Title}'.";
                }

                var tcs = new TaskCompletionSource<string>();

                // Call the AI service and log the process
                System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] Requesting AI content for unit: {unit.Title} with theoryContent: {theoryContent}");

                await _aiService.GenerateUnitContent(unit.Title, theoryContent, content =>
                {
                    System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent Callback] Received content for unit: {unit.Title}. Content: {content}");
                    tcs.SetResult(content);
                });


                // Await the result
                string aiContent = await tcs.Task;
                System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] AI content awaited for unit: {unit.Title}: {aiContent}");

                unit.UnitContent = string.IsNullOrWhiteSpace(aiContent)
                    ? "Default generated content."
                    : aiContent;

                // Update the unit in the collection to refresh UI
                int index = Units.IndexOf(Units.FirstOrDefault(u => u.Id == unit.Id));
                if (index >= 0)
                {
                    Units[index] = unit;
                }

                System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] Finished updating unit: {unit.Title} with UnitContent: {unit.UnitContent}");
                NotificationService.LogToFile($"Generated content for unit: {unit.Title}");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error generating content for unit {unit.Title}: {ex.Message}");
                NotificationService.LogToFile($"[ERROR] GenerateUnitContent for {unit.Title}: {ex}");
                unit.UnitContent = "Failed to generate content. Please try again later.";
                
                // Still update the unit in collection to show error
                int index = Units.IndexOf(Units.FirstOrDefault(u => u.Id == unit.Id));
                if (index >= 0)
                {
                    Units[index] = unit;
                }
            }
        }

        public async Task<LearningPath?> SaveLearningPathToDatabase(string aiOutput)
        {
            try
            {
                string jsonContent = AIOutputJsonExtractor.ExtractJson(aiOutput);
                AiResponse = jsonContent;

                if (string.IsNullOrEmpty(jsonContent) || !IsValidJson(jsonContent))
                {
                    NotificationService.Error("Failed to parse AI response as valid JSON");
                    NotificationService.LogToFile($"[ERROR] Invalid JSON from AI: {aiOutput}");
                    return null;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var learningPath = JsonSerializer.Deserialize<LearningPath>(jsonContent, options);

                if (learningPath == null)
                {
                    NotificationService.Error("Failed to deserialize learning path data");
                    return null;
                }

                // Ensure unique IDs
                if (learningPath.Id == Guid.Empty)
                {
                    learningPath.Id = Guid.NewGuid();
                }

                // Ensure UnitNumber is properly assigned and Ids are unique
                int unitIndex = 1;
                foreach (var unit in learningPath.Units)
                {
                    unit.LearningPathId = learningPath.Id;
                    unit.UnitNumber = unitIndex++;

                    if (unit.Id == Guid.Empty)
                    {
                        unit.Id = Guid.NewGuid();
                    }
                }

                await _databaseService.AddLearningPath(learningPath);
                NotificationService.LogToFile($"Learning path saved to database: {learningPath.Title} with {learningPath.Units.Count} units");
                return learningPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving learning path: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                }
                NotificationService.Error($"Error saving learning path: {ex.Message}");
                NotificationService.LogToFile($"[ERROR] SaveLearningPathToDatabase: {ex}");
                return null;
            }
        }

        public async Task SaveUnitToDatabase(Unit unit)
        {
            try
            {
                using var db = new LearningPathContext();

                var existingUnit = await db.Units.AsTracking().FirstOrDefaultAsync(u => u.Id == unit.Id);
                if (existingUnit != null)
                {
                    // Update only the changed fields
                    existingUnit.UnitContent = unit.UnitContent;
                    db.Entry(existingUnit).State = EntityState.Modified;
                }
                else
                {
                    // If the unit doesn't exist, add it
                    db.Units.Add(unit);
                }

                await db.SaveChangesAsync();
                NotificationService.LogToFile($"Unit saved to database: {unit.Title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving unit: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.ToString());
                }
                NotificationService.Error($"Error saving unit: {ex.Message}");
                NotificationService.LogToFile($"[ERROR] SaveUnitToDatabase for {unit.Title}: {ex}");
            }
        }

        private void ContinueToUnitOverview(Guid learningPathId)
        {
            _navigationService.NavigateTo(new UnitOverviewViewModel(_navigationService, learningPathId));
        }

        private bool IsValidJson(string jsonContent)
        {
            try
            {
                using (var document = JsonDocument.Parse(jsonContent))
                {
                    return true;
                }
            }
            catch (JsonException ex)
            {
                NotificationService.LogToFile($"[ERROR] Invalid JSON: {ex.Message}, Content: {jsonContent}");
                return false;
            }
        }
    }
}
