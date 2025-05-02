using System;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MnemoProject.Models;

public class AppSettings : INotifyPropertyChanged
{
    private static readonly string SettingsDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static AppSettings? _instance;
    public static AppSettings Instance => _instance ??= Load();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, string propertyName, Action? callback = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        callback?.Invoke();
        return true;
    }

    private string _selectedLanguage = "English";
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set 
        {
            System.Diagnostics.Debug.WriteLine($"Setting SelectedLanguage to: {value}");
            SetProperty(ref _selectedLanguage, value, nameof(SelectedLanguage), Save);
        }
    }

    private string _selectedTheme = "Dark";
    public string SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value, nameof(SelectedTheme), Save);
    }

    private bool _animationToggle = true;
    public bool AnimationToggle
    {
        get => _animationToggle;
        set => SetProperty(ref _animationToggle, value, nameof(AnimationToggle), Save);
    }

    // AI Features settings
    private bool _aiEnabled = false;
    public bool AIEnabled
    {
        get => _aiEnabled;
        set => SetProperty(ref _aiEnabled, value, nameof(AIEnabled), Save);
    }

    private string _aiProvider = "OpenAI (GPT-4)";
    public string AIProvider
    {
        get => _aiProvider;
        set => SetProperty(ref _aiProvider, value, nameof(AIProvider), Save);
    }

    private string _apiKey = string.Empty;
    public string APIKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value, nameof(APIKey), Save);
    }

    // AI Features toggles
    private bool _autoFlashcardGeneration = true;
    public bool AutoFlashcardGeneration
    {
        get => _autoFlashcardGeneration;
        set => SetProperty(ref _autoFlashcardGeneration, value, nameof(AutoFlashcardGeneration), Save);
    }

    private bool _smartContentSuggestions = true;
    public bool SmartContentSuggestions
    {
        get => _smartContentSuggestions;
        set => SetProperty(ref _smartContentSuggestions, value, nameof(SmartContentSuggestions), Save);
    }

    private bool _quizGenerationFromNotes = true;
    public bool QuizGenerationFromNotes
    {
        get => _quizGenerationFromNotes;
        set => SetProperty(ref _quizGenerationFromNotes, value, nameof(QuizGenerationFromNotes), Save);
    }

    private bool _studyScheduleOptimization = false;
    public bool StudyScheduleOptimization
    {
        get => _studyScheduleOptimization;
        set => SetProperty(ref _studyScheduleOptimization, value, nameof(StudyScheduleOptimization), Save);
    }

    // Content Generation settings
    private int _responseQuality = 3;
    public int ResponseQuality
    {
        get => _responseQuality;
        set => SetProperty(ref _responseQuality, value, nameof(ResponseQuality), Save);
    }

    private string _contentTypePreference = "Balanced";
    public string ContentTypePreference
    {
        get => _contentTypePreference;
        set => SetProperty(ref _contentTypePreference, value, nameof(ContentTypePreference), Save);
    }

    private string _responseLanguage = "Same as application language";
    public string ResponseLanguage
    {
        get => _responseLanguage;
        set => SetProperty(ref _responseLanguage, value, nameof(ResponseLanguage), Save);
    }

    // Usage & Privacy settings
    private bool _processDataLocally = true;
    public bool ProcessDataLocally
    {
        get => _processDataLocally;
        set => SetProperty(ref _processDataLocally, value, nameof(ProcessDataLocally), Save);
    }

    private bool _saveAIUsageData = false;
    public bool SaveAIUsageData
    {
        get => _saveAIUsageData;
        set => SetProperty(ref _saveAIUsageData, value, nameof(SaveAIUsageData), Save);
    }

    // Export Options
    private string _defaultExportFormat = "Standard (.fcs)";
    public string DefaultExportFormat
    {
        get => _defaultExportFormat;
        set => SetProperty(ref _defaultExportFormat, value, nameof(DefaultExportFormat), Save);
    }

    // Study Session settings
    private int _cardsPerSession = 15;
    public int CardsPerSession
    {
        get => _cardsPerSession;
        set => SetProperty(ref _cardsPerSession, value, nameof(CardsPerSession), Save);
    }

    private bool _autoPlayAudio = false;
    public bool AutoPlayAudio
    {
        get => _autoPlayAudio;
        set => SetProperty(ref _autoPlayAudio, value, nameof(AutoPlayAudio), Save);
    }

    private bool _preloadMultimedia = true;
    public bool PreloadMultimedia
    {
        get => _preloadMultimedia;
        set => SetProperty(ref _preloadMultimedia, value, nameof(PreloadMultimedia), Save);
    }

    // Auto-Play Mode settings
    private string _autoPlayMode = "Interval";
    public string AutoPlayMode
    {
        get => _autoPlayMode;
        set => SetProperty(ref _autoPlayMode, value, nameof(AutoPlayMode), Save);
    }

    private int _intervalDuration = 5;
    public int IntervalDuration
    {
        get => _intervalDuration;
        set => SetProperty(ref _intervalDuration, value, nameof(IntervalDuration), Save);
    }

    // Review Reminders settings
    private bool _enableReviewReminders = true;
    public bool EnableReviewReminders
    {
        get => _enableReviewReminders;
        set => SetProperty(ref _enableReviewReminders, value, nameof(EnableReviewReminders), Save);
    }

    private string _quietHoursStart = "8:00 PM";
    public string QuietHoursStart
    {
        get => _quietHoursStart;
        set => SetProperty(ref _quietHoursStart, value, nameof(QuietHoursStart), Save);
    }

    private string _quietHoursEnd = "7:00 AM";
    public string QuietHoursEnd
    {
        get => _quietHoursEnd;
        set => SetProperty(ref _quietHoursEnd, value, nameof(QuietHoursEnd), Save);
    }

    // Widget configuration
    private List<WidgetConfig> _enabledWidgets = new();
    public List<WidgetConfig> EnabledWidgets
    {
        get => _enabledWidgets;
        set => SetProperty(ref _enabledWidgets, value, nameof(EnabledWidgets), Save);
    }

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        return new AppSettings();
    }

    public static async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings asynchronously: {ex.Message}");
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
            
            System.Diagnostics.Debug.WriteLine($"Settings saved. SelectedLanguage is now: {SelectedLanguage}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings asynchronously: {ex.Message}");
        }
    }
}

// Widget configuration for storage in settings
public class WidgetConfig
{
    public WidgetType Type { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}
