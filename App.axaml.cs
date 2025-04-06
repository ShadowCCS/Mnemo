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

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        //using (var db = new LearningPathContext())
        //{
        //    db.Database.EnsureCreated();
        //}

    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize services
        var overlayService = OverlayService.Instance;
        var widgetService = WidgetService.Instance;
        var statisticsService = StatisticsService.Instance;
        
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
                    // Load widget settings in background
                    await widgetService.LoadSettingsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading settings: {ex.Message}");
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
