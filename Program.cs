using Avalonia;
using System;
using System.Reflection;

namespace MnemoProject;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // DEBUG: Print all embedded resources before starting the app
        System.Diagnostics.Debug.WriteLine("==== EMBEDDED RESOURCES IN ASSEMBLY ====");
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        
        foreach (var name in resourceNames)
        {
            System.Diagnostics.Debug.WriteLine($"Resource: {name}");
        }
        System.Diagnostics.Debug.WriteLine("=======================================");
        
        // Start the application
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
