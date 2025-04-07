using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;
using Avalonia.Controls;

namespace MnemoProject.ViewModels
{
    public class Settings_ExperimentalViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private ComboBoxItem _aiProvider;
        private ComboBoxItem _responseLanguage;

        public Settings_ExperimentalViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            LoadUserSettings();
        }

        public AppSettings Settings => AppSettings.Instance;

        private void LoadUserSettings()
        {
            // Load all settings from AppSettings instance
            AIEnabled = Settings.AIEnabled;
            APIKey = Settings.APIKey;
            AutoFlashcardGeneration = Settings.AutoFlashcardGeneration;
            SmartContentSuggestions = Settings.SmartContentSuggestions;
            QuizGenerationFromNotes = Settings.QuizGenerationFromNotes;
            StudyScheduleOptimization = Settings.StudyScheduleOptimization;
            ResponseQuality = Settings.ResponseQuality;
            ContentTypePreference = Settings.ContentTypePreference;
            ProcessDataLocally = Settings.ProcessDataLocally;
            SaveAIUsageData = Settings.SaveAIUsageData;
            // The ComboBox items (AIProvider and ResponseLanguage) will be set through InitializeComboBoxSelections
        }

        public void InitializeComboBoxSelections(ComboBox aiProviderComboBox, ComboBox responseLanguageComboBox)
        {
            System.Diagnostics.Debug.WriteLine($"Initializing ComboBox Selections - AIProvider: {Settings.AIProvider}, ResponseLanguage: {Settings.ResponseLanguage}");
            
            if (aiProviderComboBox != null)
            {
                System.Diagnostics.Debug.WriteLine($"AIProviderComboBox has {aiProviderComboBox.Items.Count} items");
                // Find and select the ComboBoxItem with content matching settings
                foreach (ComboBoxItem item in aiProviderComboBox.Items)
                {
                    string itemContent = item.Content?.ToString();
                    System.Diagnostics.Debug.WriteLine($"Checking item: {itemContent} against {Settings.AIProvider}");
                    if (itemContent == Settings.AIProvider)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found matching AIProvider: {itemContent}");
                        aiProviderComboBox.SelectedItem = item;
                        _aiProvider = item; // Set the backing field directly
                        break;
                    }
                }
            }
            
            if (responseLanguageComboBox != null)
            {
                System.Diagnostics.Debug.WriteLine($"ResponseLanguageComboBox has {responseLanguageComboBox.Items.Count} items");
                foreach (ComboBoxItem item in responseLanguageComboBox.Items)
                {
                    string itemContent = item.Content?.ToString();
                    System.Diagnostics.Debug.WriteLine($"Checking item: {itemContent} against {Settings.ResponseLanguage}");
                    if (itemContent == Settings.ResponseLanguage)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found matching ResponseLanguage: {itemContent}");
                        responseLanguageComboBox.SelectedItem = item;
                        _responseLanguage = item; // Set the backing field directly
                        break;
                    }
                }
            }
            
            // Notify that these properties have changed
            OnPropertyChanged(nameof(AIProvider));
            OnPropertyChanged(nameof(ResponseLanguage));
        }

        public bool AIEnabled
        {
            get => Settings.AIEnabled;
            set
            {
                System.Diagnostics.Debug.WriteLine($"Setting AIEnabled to {value}, current: {Settings.AIEnabled}");
                if (Settings.AIEnabled != value)
                {
                    Settings.AIEnabled = value;
                    OnPropertyChanged();
                    System.Diagnostics.Debug.WriteLine($"AIEnabled updated to {value}");
                }
            }
        }

        public ComboBoxItem AIProvider
        {
            get => _aiProvider;
            set
            {
                _aiProvider = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    string content = value.Content?.ToString();
                    if (content != null && Settings.AIProvider != content)
                    {
                        Settings.AIProvider = content;
                    }
                }
            }
        }

        public string APIKey
        {
            get => Settings.APIKey;
            set
            {
                if (Settings.APIKey != value)
                {
                    Settings.APIKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoFlashcardGeneration
        {
            get => Settings.AutoFlashcardGeneration;
            set
            {
                System.Diagnostics.Debug.WriteLine($"Setting AutoFlashcardGeneration to {value}, current: {Settings.AutoFlashcardGeneration}");
                if (Settings.AutoFlashcardGeneration != value)
                {
                    Settings.AutoFlashcardGeneration = value;
                    OnPropertyChanged();
                    System.Diagnostics.Debug.WriteLine($"AutoFlashcardGeneration updated to {value}");
                }
            }
        }

        public bool SmartContentSuggestions
        {
            get => Settings.SmartContentSuggestions;
            set
            {
                System.Diagnostics.Debug.WriteLine($"Setting SmartContentSuggestions to {value}, current: {Settings.SmartContentSuggestions}");
                if (Settings.SmartContentSuggestions != value)
                {
                    Settings.SmartContentSuggestions = value;
                    OnPropertyChanged();
                    System.Diagnostics.Debug.WriteLine($"SmartContentSuggestions updated to {value}");
                }
            }
        }

        public bool QuizGenerationFromNotes
        {
            get => Settings.QuizGenerationFromNotes;
            set
            {
                if (Settings.QuizGenerationFromNotes != value)
                {
                    Settings.QuizGenerationFromNotes = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StudyScheduleOptimization
        {
            get => Settings.StudyScheduleOptimization;
            set
            {
                if (Settings.StudyScheduleOptimization != value)
                {
                    Settings.StudyScheduleOptimization = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ResponseQuality
        {
            get => Settings.ResponseQuality;
            set
            {
                if (Settings.ResponseQuality != value)
                {
                    Settings.ResponseQuality = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ContentTypePreference
        {
            get => Settings.ContentTypePreference;
            set
            {
                if (Settings.ContentTypePreference != value)
                {
                    Settings.ContentTypePreference = value;
                    OnPropertyChanged();
                }
            }
        }

        public ComboBoxItem ResponseLanguage
        {
            get => _responseLanguage;
            set
            {
                _responseLanguage = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    string content = value.Content?.ToString();
                    if (content != null && Settings.ResponseLanguage != content)
                    {
                        Settings.ResponseLanguage = content;
                    }
                }
            }
        }

        public bool ProcessDataLocally
        {
            get => Settings.ProcessDataLocally;
            set
            {
                if (Settings.ProcessDataLocally != value)
                {
                    Settings.ProcessDataLocally = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SaveAIUsageData
        {
            get => Settings.SaveAIUsageData;
            set
            {
                if (Settings.SaveAIUsageData != value)
                {
                    Settings.SaveAIUsageData = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
