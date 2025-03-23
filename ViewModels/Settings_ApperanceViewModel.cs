using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using MnemoProject.Models;
using System.Runtime.CompilerServices;

namespace MnemoProject.ViewModels
{
    public class Settings_ApperanceViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private LanguageItem? _selectedLanguageItem;
        private ThemeItem? _selectedThemeItem;
        private bool _animationToggle;

        public Settings_ApperanceViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            InitializeCollections();
            LoadUserSettings();
        }

        public AppSettings Settings => AppSettings.Instance;

        public ObservableCollection<LanguageItem> Languages { get; private set; } = new();

        public LanguageItem? SelectedLanguageItem
        {
            get => _selectedLanguageItem;
            set
            {
                if (SetProperty(ref _selectedLanguageItem, value))
                {
                    Settings.SelectedLanguage = value?.Name ?? "English";
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
            Languages = new ObservableCollection<LanguageItem>
            {
                new("English", "Assets/Icons/english.png"),
                new("Spanish", "Assets/Icons/spanish.png"),
                new("German", "Assets/Icons/german.png")
            };

            Themes = new ObservableCollection<ThemeItem>
            {
                new("Dark", "#262626"),
                new("Light", "#bebebe"),
                new("Nature", "#526935")
            };
        }

        private void LoadUserSettings()
        {
            _animationToggle = Settings.AnimationToggle;
            _selectedLanguageItem = Languages.FirstOrDefault(l => l.Name == Settings.SelectedLanguage);
            _selectedThemeItem = Themes.FirstOrDefault(t => t.Name == Settings.SelectedTheme);
        }
    }
}