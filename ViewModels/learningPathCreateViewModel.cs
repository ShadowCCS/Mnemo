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

        // Constructor no longer requires external AI dependencies
        public learningPathCreateViewModel(NavigationService navigationService, string userInput)
        {
            _navigationService = navigationService;

            // Instantiate AI dependencies inside the ViewModel
            var aiProvider = new GeminiProvider();  // Uses ApiKeyManager
            var aiWorker = new AIWorker();
            _aiService = new AIService(aiProvider, aiWorker);

            _userInput = userInput;

            GenStatus = "Learning Path...";
            GenerateLearningPath();
        }


        public void GenerateLearningPath()
        {
            _aiService.GenerateLearningPathOutline(UserInput, async content =>
            {
                await SaveLearningPathToDatabase(content);
                GenStatus = "Unit 1...";
                GenerateUnitContent("Rockets", "Introduction To Rockets");
            });
        }

        public void GenerateUnitContent(string userInput, string unitTitle)
        {
            _aiService.GenerateUnitContent(userInput, unitTitle, content =>
            {
                System.Diagnostics.Debug.WriteLine("Unit: " + content);
                ContinueToUnitOverview();
            });
        }

        public async Task SaveLearningPathToDatabase(string aiOutput)
        {
            try
            {
                // Extract the JSON content from the AI output using the utility class
                string jsonContent = AIOutputJsonExtractor.ExtractJson(aiOutput);
                AiResponse = jsonContent;
                System.Diagnostics.Debug.WriteLine(AiResponse);

                if (string.IsNullOrEmpty(jsonContent))
                {
                    System.Diagnostics.Debug.WriteLine("No valid JSON found in the AI output.");
                    return;
                }

                // Validate JSON structure
                if (!IsValidJson(jsonContent))
                {
                    System.Diagnostics.Debug.WriteLine("Invalid JSON structure.");
                    return;
                }

                // Set JsonSerializerOptions to make property names case-insensitive
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Try to deserialize and save the learning path
                var learningPath = JsonSerializer.Deserialize<LearningPath>(jsonContent, options);
                if (learningPath != null)
                {
                    await _databaseService.AddLearningPath(learningPath);
                    System.Diagnostics.Debug.WriteLine("Learning Path saved successfully.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Deserialized learning path is null.");
                }
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine("JSON Error: " + jsonEx.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + jsonEx.StackTrace);
            }
            catch (SqliteException sqliteEx)
            {
                System.Diagnostics.Debug.WriteLine("SQLite Error: " + sqliteEx.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + sqliteEx.StackTrace);
            }
            catch (DbUpdateException dbUpdateEx)
            {
                System.Diagnostics.Debug.WriteLine("DB Update Error: " + dbUpdateEx.Message);
                if (dbUpdateEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner Exception: " + dbUpdateEx.InnerException.Message);
                    System.Diagnostics.Debug.WriteLine("Inner Stack Trace: " + dbUpdateEx.InnerException.StackTrace);
                }
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + dbUpdateEx.StackTrace);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("General Error: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }

        private void ContinueToUnitOverview()
        {
            _navigationService.NavigateTo(new UnitOverviewViewModel(_navigationService));
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
