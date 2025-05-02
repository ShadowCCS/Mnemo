using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using MnemoProject.ViewModels;
using MnemoProject.Views;
using MnemoProject.Data;
using MnemoProject.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MnemoProject;

// Add ZIndex constants for consistent use across the application
public static class ZIndexes
{
    public const int Default = 0;
    public const int Notifications = 1;
    public const int Overlays = 2;
}

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;
    private static Task _databaseInitTask;
    private static readonly string LanguagesDirectory = Path.Combine(AppContext.BaseDirectory, "Languages");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Ensure languages directory exists
        EnsureLanguagesDirectory();

        // Start database initialization as early as possible
        _databaseInitTask = Task.Run(async () => 
        {
            try
            {
                var dbService = new DatabaseService();
                await dbService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("Database initialized at application startup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing database at startup: {ex.Message}");
            }
        });
    }

    private void EnsureLanguagesDirectory()
    {
        try
        {
            // Ensure languages directory exists
            Directory.CreateDirectory(LanguagesDirectory);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error ensuring languages directory: {ex.Message}");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize services
        var overlayService = OverlayService.Instance;
        var widgetService = WidgetService.Instance;
        var statisticsService = StatisticsService.Instance;
        
        // Initialize localization service
        var localizationService = LocalizationService.Instance;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktopLifetime = desktop;

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            // Load settings asynchronously to avoid blocking the UI
            _ = Task.Run(async () => 
            {
                try 
                {
                    // Wait for database initialization before loading settings
                    if (_databaseInitTask != null)
                    {
                        await _databaseInitTask;
                    }
                    
                    // Load widget settings in background
                    await widgetService.LoadSettingsAsync();
                    
                    // Force refresh widget data to ensure statistics are loaded
                    widgetService.ForceRefreshWidgetData();
                    
                    // Log that we've completed initialization
                    LogService.Log.Info("Application initialization completed, widgets refreshed with latest statistics");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }


    public void Shutdown()
    {
        _desktopLifetime?.Shutdown();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
