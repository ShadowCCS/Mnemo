using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Reflection;
using System.Threading;

namespace MnemoProject.Services
{
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private ResourceManager? _resourceManager;
        
        public event EventHandler? CultureChanged;
        
        private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;
        public CultureInfo CurrentCulture => _currentCulture;
        
        // Constants for resource keys
        private const string LANGUAGE_NATIVE_NAME_KEY = "__LanguageNativeName";
        
        private LocalizationService()
        {
            _resourceManager = new ResourceManager("MnemoProject.Languages.Strings", Assembly.GetExecutingAssembly());
            
            // Initialize with the selected language
            var languageCode = AppSettings.Instance.SelectedLanguage;
            ChangeCulture(GetCultureInfoFromLanguage(languageCode));
        }
        
        /// <summary>
        /// Gets a localized string from .resx files
        /// </summary>
        /// <param name="key">The resource key</param>
        /// <param name="defaultValue">The default value if not found</param>
        /// <returns>The localized string</returns>
        public string GetString(string key, string defaultValue = "")
        {
            try
            {
                if (_resourceManager != null)
                {
                    var resValue = _resourceManager.GetString(key, _currentCulture);
                    if (!string.IsNullOrEmpty(resValue))
                    {
                        return resValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting localized string: {ex.Message}");
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Changes the current culture used for localization
        /// </summary>
        public void ChangeCulture(CultureInfo cultureInfo)
        {
            if (_currentCulture.Equals(cultureInfo)) return;
            
            _currentCulture = cultureInfo;
            
            // Update the current thread's culture
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            
            // Update the app settings
            AppSettings.Instance.SelectedLanguage = GetLanguageNameFromCulture(cultureInfo);
            AppSettings.Instance.Save();
            
            // Notify subscribers
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Gets the language name (e.g., "English") from a CultureInfo
        /// </summary>
        private string GetLanguageNameFromCulture(CultureInfo cultureInfo)
        {
            // For now, just return the English display name of the culture
            return cultureInfo.DisplayName;
        }
        
        /// <summary>
        /// Gets the CultureInfo from a language name
        /// </summary>
        public CultureInfo GetCultureInfoFromLanguage(string language)
        {
            try
            {
                // First try to treat it as a culture name directly
                try
                {
                    return new CultureInfo(language);
                }
                catch
                {
                    // If that fails, it might be a language name like "English"
                    // Try to find the corresponding culture
                    foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                    {
                        if (culture.EnglishName.Equals(language, StringComparison.OrdinalIgnoreCase) ||
                            culture.NativeName.Equals(language, StringComparison.OrdinalIgnoreCase) ||
                            culture.DisplayName.Equals(language, StringComparison.OrdinalIgnoreCase))
                        {
                            return culture;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting culture info: {ex.Message}");
            }
            
            // Fallback to English
            return new CultureInfo("en");
        }
        
        /// <summary>
        /// Gets the language name in its native form (e.g., "Deutsch" for German, "Español" for Spanish)
        /// </summary>
        public string GetNativeLanguageName(string cultureCode)
        {
            try
            {
                var culture = new CultureInfo(cultureCode);
                
                // First try to get the native name from the resource file itself
                // This allows language pack creators to specify their own native name
                string nativeNameFromResources = GetString(LANGUAGE_NATIVE_NAME_KEY, "", new CultureInfo(cultureCode));
                if (!string.IsNullOrEmpty(nativeNameFromResources))
                {
                    System.Diagnostics.Debug.WriteLine($"Found native name '{nativeNameFromResources}' for culture {cultureCode} from resources");
                    return nativeNameFromResources;
                }
                
                // If not found in resources, return the native name from the culture
                // Get the display name in the target culture itself
                string nativeName = culture.NativeName;
                
                // For most cultures, the native name includes the country.
                // We usually just want the language part, which is before the first space or parenthesis
                if (nativeName.Contains(" ("))
                {
                    nativeName = nativeName.Split(" (")[0];
                }
                else if (nativeName.Contains(" "))
                {
                    nativeName = nativeName.Split(' ')[0];
                }
                
                return nativeName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting native name for {cultureCode}: {ex.Message}");
                // Fallback
                return cultureCode;
            }
        }
        
        /// <summary>
        /// Gets a localized string from .resx files using a specific culture
        /// </summary>
        public string GetString(string key, string defaultValue, CultureInfo culture)
        {
            try
            {
                if (_resourceManager != null)
                {
                    var resValue = _resourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(resValue))
                    {
                        return resValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting localized string: {ex.Message}");
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Gets all available cultures based on hardcoded list
        /// </summary>
        public List<CultureInfo> GetAvailableCultures()
        {
            System.Diagnostics.Debug.WriteLine("GetAvailableCultures: Using hardcoded cultures list");
            
            var result = new List<CultureInfo>();
            
            try 
            {
                // Hardcoded list of supported languages with their cultures
                var supportedLanguages = new[] 
                { 
                    "en", // English
                    "de", // German
                    "es", // Spanish
                    "fr"  // French
                };
                
                foreach (var language in supportedLanguages)
                {
                    try
                    {
                        var culture = new CultureInfo(language);
                        result.Add(culture);
                        System.Diagnostics.Debug.WriteLine($"GetAvailableCultures: Added culture: {culture.DisplayName} ({culture.Name})");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating culture for {language}: {ex.Message}");
                    }
                }
                
                // Log summary of found cultures
                System.Diagnostics.Debug.WriteLine($"GetAvailableCultures: Added {result.Count} cultures");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAvailableCultures: {ex.Message}");
                
                // Fallback to just English if there's an error
                if (result.Count == 0)
                {
                    result.Add(new CultureInfo("en"));
                    System.Diagnostics.Debug.WriteLine("GetAvailableCultures: Added English as fallback after error");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets all available cultures with their native names
        /// </summary>
        public Dictionary<string, string> GetAvailableLanguages()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var culture in GetAvailableCultures())
            {
                result[culture.Name] = GetNativeLanguageName(culture.Name);
            }
            
            System.Diagnostics.Debug.WriteLine($"GetAvailableLanguages: Found {result.Count} languages:");
            foreach (var lang in result)
            {
                System.Diagnostics.Debug.WriteLine($"  - {lang.Key}: {lang.Value}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Forces a refresh of available cultures
        /// </summary>
        public void RefreshCultures()
        {
            System.Diagnostics.Debug.WriteLine("Forcing refresh of available cultures");
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
    }
} 