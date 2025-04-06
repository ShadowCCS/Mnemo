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
        set => SetProperty(ref _selectedLanguage, value, nameof(SelectedLanguage), Save);
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
            Console.WriteLine($"Error loading settings: {ex.Message}");
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
            Console.WriteLine($"Error loading settings asynchronously: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
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
            Console.WriteLine($"Error saving settings asynchronously: {ex.Message}");
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
