using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Text.Json.Nodes;

namespace MnemoProject.Services
{
    public class LanguageService
    {
        private static readonly string LanguagesDirectory = Path.Combine(AppContext.BaseDirectory, "Languages");
        private static readonly string DefaultLanguageCode = "en";
        
        private static LanguageService? _instance;
        public static LanguageService Instance => _instance ??= new LanguageService();
        
        private JsonNode? _currentLanguage;
        private string _currentLanguageCode = DefaultLanguageCode;
        private Dictionary<string, string> _availableLanguages = new();
        
        public string CurrentLanguageCode => _currentLanguageCode;
        public IReadOnlyDictionary<string, string> AvailableLanguages => _availableLanguages;
        
        public event EventHandler? LanguageChanged;
        
        private LanguageService()
        {
            LoadAvailableLanguages();
            LoadLanguage(AppSettings.Instance.SelectedLanguage);
        }
        
        public void Initialize()
        {
            // Nothing needed here, constructor does the work
            // This method exists to ensure the service is initialized
        }
        
        private void LoadAvailableLanguages()
        {
            _availableLanguages.Clear();
            
            try
            {
                Directory.CreateDirectory(LanguagesDirectory);
                
                foreach (var file in Directory.GetFiles(LanguagesDirectory, "*.json"))
                {
                    try
                    {
                        var languageCode = Path.GetFileNameWithoutExtension(file);
                        var languageJson = JsonNode.Parse(File.ReadAllText(file));
                        
                        if (languageJson?["language"]?["name"]?.GetValue<string>() is string languageName)
                        {
                            _availableLanguages[languageCode] = languageName;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading language file: {file}, {ex.Message}");
                    }
                }
                
                if (_availableLanguages.Count == 0)
                {
                    // If no language files found, add English as default
                    _availableLanguages[DefaultLanguageCode] = "English";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available languages: {ex.Message}");
                // Ensure at least the default language is available
                _availableLanguages[DefaultLanguageCode] = "English";
            }
        }
        
        public bool LoadLanguage(string languageCode)
        {
            try
            {
                string languageFilePath = Path.Combine(LanguagesDirectory, $"{languageCode}.json");
                
                if (!File.Exists(languageFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Language file not found: {languageFilePath}");
                    
                    // If not found and not already default, try to load default language
                    if (languageCode != DefaultLanguageCode)
                    {
                        return LoadLanguage(DefaultLanguageCode);
                    }
                    
                    // Otherwise, create default language file
                    CreateDefaultLanguageFile();
                    if (File.Exists(languageFilePath))
                    {
                        _currentLanguage = JsonNode.Parse(File.ReadAllText(languageFilePath));
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    _currentLanguage = JsonNode.Parse(File.ReadAllText(languageFilePath));
                }
                
                _currentLanguageCode = languageCode;
                LanguageChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading language: {ex.Message}");
                
                // Try to load default language if not already trying
                if (languageCode != DefaultLanguageCode)
                {
                    return LoadLanguage(DefaultLanguageCode);
                }
                
                return false;
            }
        }
        
        private void CreateDefaultLanguageFile()
        {
            try
            {
                string defaultLanguageFilePath = Path.Combine(LanguagesDirectory, $"{DefaultLanguageCode}.json");
                Directory.CreateDirectory(LanguagesDirectory);
                
                // Create minimal default language file
                var defaultLanguage = new
                {
                    language = new
                    {
                        name = "English",
                        code = "en"
                    },
                    errors = new
                    {
                        languageFileNotFound = "Language file not found, using default language",
                        failedToLoadLanguage = "Failed to load language file, using default language"
                    }
                };
                
                string json = JsonSerializer.Serialize(defaultLanguage, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(defaultLanguageFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating default language file: {ex.Message}");
            }
        }
        
        public string GetString(string key, string defaultValue = "")
        {
            try
            {
                var parts = key.Split('.');
                JsonNode? current = _currentLanguage;
                
                foreach (var part in parts)
                {
                    current = current?[part];
                    if (current == null)
                    {
                        return defaultValue;
                    }
                }
                
                return current.GetValue<string>();
            }
            catch
            {
                return defaultValue;
            }
        }
        
        public void ChangeLanguage(string languageCode)
        {
            if (LoadLanguage(languageCode))
            {
                AppSettings.Instance.SelectedLanguage = languageCode;
                AppSettings.Instance.Save();
            }
        }
        
        public async Task<string> GetLanguageJsonAsync(string languageCode)
        {
            try
            {
                string languageFilePath = Path.Combine(LanguagesDirectory, $"{languageCode}.json");
                if (File.Exists(languageFilePath))
                {
                    return await File.ReadAllTextAsync(languageFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading language file: {ex.Message}");
            }
            
            return "{}";
        }
        
        public void RefreshAvailableLanguages()
        {
            LoadAvailableLanguages();
        }
    }
} 