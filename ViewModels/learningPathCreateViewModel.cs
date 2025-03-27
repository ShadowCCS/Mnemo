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

namespace MnemoProject.ViewModels
{
    partial class learningPathCreateViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private readonly AIService _aiService;
        private readonly DatabaseService _databaseService = new();

        public ObservableCollection<Unit> Units { get; } = new();

        private string _userInput = "";
        private string _aiResponse;
        private string _genStatus = "...";
        public string UserInput
        {
            get => _userInput;
            set => SetProperty(ref _userInput, value);
        }

        public string AiResponse
        {
            get => _aiResponse;
            set => SetProperty(ref _aiResponse, value);
        }

        public string GenStatus
        {
            get => _genStatus;
            set => SetProperty(ref _genStatus, value);
        }

        public learningPathCreateViewModel(NavigationService navigationService, string userInput)
        {
            _navigationService = navigationService;

            var aiProvider = new GeminiProvider();
            var aiWorker = new AIWorker();
            _aiService = new AIService(aiProvider, aiWorker);

            _userInput = userInput;

            GenStatus = "Learning Path...";
            GenerateLearningPath();
        }

        private bool _learningPathProcessed = false;

        public async Task GenerateLearningPath()
        {
            await _aiService.GenerateLearningPathOutline(UserInput, async content =>
            {
                if (_learningPathProcessed) return;
                _learningPathProcessed = true;

                var learningPath = await SaveLearningPathToDatabase(content);
                if (learningPath != null && learningPath.Units.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[GenerateLearningPath] Processing learningPath with units.");
                    int unitIndex = 1;

                    // Process the first unit separately.
                    var firstUnit = learningPath.Units.First();
                    System.Diagnostics.Debug.WriteLine($"[GenerateLearningPath] Processing first unit: {firstUnit.Title}");

                    firstUnit.LearningPathId = learningPath.Id;
                    firstUnit.UnitNumber = unitIndex++;

                    GenStatus = $"Unit {firstUnit.UnitNumber}...";

                    // Prepare theory content.
                    string theoryContent = !string.IsNullOrWhiteSpace(firstUnit.TheoryContent)
                        ? firstUnit.TheoryContent
                        : $"Generate content for '{firstUnit.Title}'.";

                    // Add the first unit immediately with a placeholder.
                    if (!Units.Contains(firstUnit))
                    {
                        firstUnit.UnitContent = "Loading content...";
                        Units.Add(firstUnit);
                        System.Diagnostics.Debug.WriteLine($"[GenerateLearningPath] Added first unit to collection with placeholder content.");
                    }

                    // Generate content and update the unit.
                    await GenerateUnitContent(learningPath, firstUnit, theoryContent);

                    // Persist the updated unit content to the database.
                    await SaveUnitToDatabase(firstUnit);

                    System.Diagnostics.Debug.WriteLine($"[GenerateLearningPath] Finished generating content for first unit: {firstUnit.Title}. UnitContent now: {firstUnit.UnitContent}");

                    // Process remaining units.
                    foreach (var unit in learningPath.Units.Skip(1))
                    {
                        unit.LearningPathId = learningPath.Id;
                        unit.UnitNumber = unitIndex++;
                        unit.Id = Guid.NewGuid(); // Ensure unique unit ID
                        unit.UnitContent = "";  // Set to empty string or a placeholder if needed.
                        Units.Add(unit);
                        System.Diagnostics.Debug.WriteLine($"[GenerateLearningPath] Added remaining unit: {unit.Title}");
                    }

                    ContinueToUnitOverview(learningPath.Id);
                }
            });
        }

        public async Task GenerateUnitContent(LearningPath learningPath, Unit unit, string theoryContent)
        {
            System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] Starting for unit: {unit.Title}");

            if (string.IsNullOrWhiteSpace(theoryContent))
            {
                theoryContent = $"Generate detailed content for the unit titled '{unit.Title}'.";
            }

            var tcs = new TaskCompletionSource<string>();

            // Call the AI service and log the process.
            System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] Requesting AI content for unit: {unit.Title} with theoryContent: {theoryContent}");

            await _aiService.GenerateUnitContent(unit.Title, theoryContent, async content =>
            {
                System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent Callback] Received content for unit: {unit.Title}. Content: {content}");
                tcs.SetResult(content);
            });

            // Await the result.
            string aiContent = await tcs.Task;
            System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] AI content awaited for unit: {unit.Title}: {aiContent}");

            unit.UnitContent = string.IsNullOrWhiteSpace(aiContent)
                ? "Default generated content."
                : aiContent;

            System.Diagnostics.Debug.WriteLine($"[GenerateUnitContent] Finished updating unit: {unit.Title} with UnitContent: {unit.UnitContent}");
        }

        public async Task<LearningPath?> SaveLearningPathToDatabase(string aiOutput)
        {
            try
            {
                string jsonContent = AIOutputJsonExtractor.ExtractJson(aiOutput);
                AiResponse = jsonContent;

                if (string.IsNullOrEmpty(jsonContent) || !IsValidJson(jsonContent))
                    return null;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var learningPath = JsonSerializer.Deserialize<LearningPath>(jsonContent, options);

                if (learningPath == null)
                    return null;

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
                return learningPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving learning path: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                }
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving unit: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.ToString());
                }
            }
        }



        private void ContinueToUnitOverview(Guid learningPathId)
        {
            _navigationService.NavigateTo(new UnitOverviewViewModel(_navigationService, learningPathId, _userInput));
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
            catch (JsonException)
            {
                return false;
            }
        }

        [RelayCommand]
        public void GoBack()
        {
            _navigationService.GoBack();
        }

        [RelayCommand]
        public void GoHome()
        {
            _navigationService.NavigateTo(new LearningPathViewModel(_navigationService));
        }
    }
}
