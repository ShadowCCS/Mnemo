using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MnemoProject.Services;

namespace MnemoProject.Helpers
{
    public class LocalizationProvider : INotifyPropertyChanged
    {
        private static LocalizationProvider? _instance;
        public static LocalizationProvider Instance => _instance ??= new LocalizationProvider();
        
        private readonly LanguageService _languageService;
        
        private LocalizationProvider()
        {
            _languageService = LanguageService.Instance;
            _languageService.LanguageChanged += OnLanguageChanged;
        }
        
        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Notify all bindings that all properties have changed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
        
        public string this[string key]
        {
            get
            {
                string value = _languageService.GetString(key, key);
                return value;
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 