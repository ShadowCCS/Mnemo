using Avalonia.Metadata;

// This XmlnsDefinition allows XAML to find the LocalizationExtension with the prefix xmlns:loc="using:MnemoProject.Helpers"
[assembly: XmlnsDefinition("using:MnemoProject.Helpers", "MnemoProject.Helpers")]
// Also add a definition for the root Avalonia URI which might be needed
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "MnemoProject.Helpers")] 