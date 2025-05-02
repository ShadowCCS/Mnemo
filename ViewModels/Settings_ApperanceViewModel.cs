using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Globalization;
using MnemoProject.Models;
using System.Runtime.CompilerServices;
using MnemoProject.Services;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using Avalonia.Threading;
using System.IO;
using System.Reflection;
using System.Resources;

namespace MnemoProject.ViewModels
{
    public class Settings_ApperanceViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private LanguageItem? _selectedLanguageItem;
        private ThemeItem? _selectedThemeItem;
        private bool _animationToggle;
        private readonly LocalizationService _localizationService;

        public Settings_ApperanceViewModel(NavigationService navigationService)
        {
            System.Diagnostics.Debug.WriteLine("Settings_ApperanceViewModel: Constructor called");
            _navigationService = navigationService;
            _localizationService = LocalizationService.Instance;
            
            // Initialize collections
            InitializeCollections();
            
            // Load user settings after languages are initialized
            LoadUserSettings();
            
            // Subscribe to culture changed event
            _localizationService.CultureChanged += (s, e) => 
            {
                System.Diagnostics.Debug.WriteLine("Settings_ApperanceViewModel: Culture changed event triggered");
                // Update UI on the UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    // Use localized names for languages
                    UpdateLanguageDisplayNames();
                    
                    // Notify that all properties might have changed
                    OnPropertyChanged(string.Empty);
                });
            };
            
            // Force a refresh to make sure the UI is updated
            Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(Languages));
            });
        }

        public AppSettings Settings => AppSettings.Instance;

        public ObservableCollection<LanguageItem> Languages { get; private set; } = new();

        public LanguageItem? SelectedLanguageItem
        {
            get => _selectedLanguageItem;
            set
            {
                if (SetProperty(ref _selectedLanguageItem, value) && value != null)
                {
                    // Save previous language to check if it changed
                    string previousLanguage = Settings.SelectedLanguage;
                    
                    // Update the culture when language changes
                    _localizationService.ChangeCulture(value.GetCulture());
                    Settings.SelectedLanguage = value.CultureCode;
                    
                    // If language actually changed, show a notification about restart
                    if (!string.Equals(previousLanguage, value.CultureCode, StringComparison.OrdinalIgnoreCase))
                    {
                        string notificationMsg = _localizationService.GetString(
                            "Settings_Language_ChangeNotification", 
                            "Please restart the application for the language change to take full effect.");
                            
                        string title = _localizationService.GetString("Settings_Language", "Language");
                        NotificationService.Warning(notificationMsg, title);
                    }
                    
                    // Notify that all items in the UI may need to refresh due to localization change
                    OnPropertyChanged(string.Empty);
                }
            }
        }

        public ObservableCollection<ThemeItem> Themes { get; private set; } = new();

        public ThemeItem? SelectedThemeItem
        {
            get => _selectedThemeItem;
            set
            {
                if (SetProperty(ref _selectedThemeItem, value))
                {
                    Settings.SelectedTheme = value?.Name ?? "Dark";
                }
            }
        }

        public bool AnimationToggle
        {
            get => _animationToggle;
            set
            {
                if (SetProperty(ref _animationToggle, value))
                {
                    Settings.AnimationToggle = value;
                }
            }
        }

        private void InitializeCollections()
        {
            System.Diagnostics.Debug.WriteLine("InitializeCollections: Starting initialization");
            Languages.Clear();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("InitializeCollections: Starting to load languages");
                
                // Get all available languages from embedded resources
                var resourceLanguages = GetAvailableLanguagesFromResources();
                System.Diagnostics.Debug.WriteLine($"InitializeCollections: Found {resourceLanguages.Count} language resources");
                
                // Debug: Output all available resources
                System.Diagnostics.Debug.WriteLine("DEBUG - Available language resources:");
                foreach (var lang in resourceLanguages)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {lang}");
                }
                
                // Create cultures from resource languages
                var cultures = new List<CultureInfo>();
                foreach (var langCode in resourceLanguages)
                {
                    try
                    {
                        var culture = new CultureInfo(langCode);
                        cultures.Add(culture);
                        System.Diagnostics.Debug.WriteLine($"Added culture: {culture.DisplayName} ({culture.Name})");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating culture for {langCode}: {ex.Message}");
                    }
                }
                
                // Clear and rebuild all language items
                Languages.Clear();
                
                System.Diagnostics.Debug.WriteLine("=== Languages being added to dropdown ===");
                
                foreach (var culture in cultures)
                {
                    System.Diagnostics.Debug.WriteLine($"Processing culture: {culture.DisplayName} ({culture.Name})");
                    
                    // Get the native language name from resources
                    string nativeName = _localizationService.GetNativeLanguageName(culture.Name);
                    System.Diagnostics.Debug.WriteLine($"  - Native name: '{nativeName}'");
                    
                    // Create language item and add to collection (no icon)
                    var languageItem = new LanguageItem(culture.DisplayName, string.Empty, culture.Name, nativeName);
                    Languages.Add(languageItem);
                    System.Diagnostics.Debug.WriteLine($"  - Added '{languageItem.NativeName}' ({culture.Name})");
                }
                
                System.Diagnostics.Debug.WriteLine($"InitializeCollections: Added {Languages.Count} languages to collection");
                
                // Debug: Output all languages in the collection
                System.Diagnostics.Debug.WriteLine("DEBUG - Languages in collection:");
                foreach (var lang in Languages)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {lang.Name} ({lang.CultureCode}), NativeName: {lang.NativeName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeCollections: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                
                // Add English as fallback
                if (Languages.Count == 0)
                {
                    try
                    {
                        var englishCulture = new CultureInfo("en");
                        Languages.Add(new LanguageItem("English", string.Empty, "en", "English"));
                        System.Diagnostics.Debug.WriteLine("Added English as fallback");
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding fallback English: {innerEx.Message}");
                    }
                }
            }

            Themes = new ObservableCollection<ThemeItem>
            {
                new("Dark", "#262626"),
                new("Light", "#bebebe"),
                new("Nature", "#526935")
            };
        }
        
        /// <summary>
        /// Get all available languages by scanning the resource files
        /// </summary>
        private List<string> GetAvailableLanguagesFromResources()
        {
            System.Diagnostics.Debug.WriteLine("GetAvailableLanguagesFromResources: Using hardcoded language list");
            
            // Hardcoded list of supported languages
            var languages = new List<string>
            {
                "en", // English (default)
                "de", // German
                "es", // Spanish
                "fr"  // French
            };
            
            System.Diagnostics.Debug.WriteLine($"GetAvailableLanguagesFromResources: Added {languages.Count} languages");
            foreach (var lang in languages)
            {
                System.Diagnostics.Debug.WriteLine($"  - {lang}");
            }
            
            return languages;
        }
        
        /// <summary>
        /// Updates the display names of languages based on the current culture
        /// </summary>
        private void UpdateLanguageDisplayNames()
        {
            try
            {
                foreach (var lang in Languages)
                {
                    // Update the native name for this language
                    lang.NativeName = _localizationService.GetNativeLanguageName(lang.CultureCode);
                }
                
                // Force refresh the Languages collection
                OnPropertyChanged(nameof(Languages));
                OnPropertyChanged(nameof(SelectedLanguageItem));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating language display names: {ex.Message}");
            }
        }

        private void LoadUserSettings()
        {
            _animationToggle = Settings.AnimationToggle;
            
            System.Diagnostics.Debug.WriteLine($"LoadUserSettings: Settings.SelectedLanguage = {Settings.SelectedLanguage}");
            
            // Find language item by culture code first
            _selectedLanguageItem = Languages.FirstOrDefault(l => 
                l.CultureCode.Equals(Settings.SelectedLanguage, StringComparison.OrdinalIgnoreCase));
            
            if (_selectedLanguageItem != null)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUserSettings: Found by culture code: {_selectedLanguageItem.Name}");
            }
                
            // If not found, try by name
            if (_selectedLanguageItem == null)
            {
                _selectedLanguageItem = Languages.FirstOrDefault(l => 
                    l.Name.Equals(Settings.SelectedLanguage, StringComparison.OrdinalIgnoreCase));
                    
                if (_selectedLanguageItem != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadUserSettings: Found by name: {_selectedLanguageItem.Name}");
                }
            }
            
            // If still not found, default to English
            if (_selectedLanguageItem == null)
            {
                _selectedLanguageItem = Languages.FirstOrDefault(l => 
                    l.CultureCode.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                    l.Name.Contains("English", StringComparison.OrdinalIgnoreCase));
                    
                if (_selectedLanguageItem != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadUserSettings: Using English default: {_selectedLanguageItem.Name}");
                    // Update settings with the English culture code
                    Settings.SelectedLanguage = _selectedLanguageItem.CultureCode;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadUserSettings: Couldn't find any language item");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"LoadUserSettings: SelectedLanguageItem set to: {_selectedLanguageItem?.Name ?? "null"}");
            
            _selectedThemeItem = Themes.FirstOrDefault(t => t.Name == Settings.SelectedTheme);
        }
    }
}