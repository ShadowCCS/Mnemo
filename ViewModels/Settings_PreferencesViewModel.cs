using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;
using Avalonia.Controls;

namespace MnemoProject.ViewModels
{
    public class Settings_PreferencesViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private ComboBoxItem _defaultExportFormat;
        private ComboBoxItem _autoPlayMode;
        private ComboBoxItem _quietHoursStart;
        private ComboBoxItem _quietHoursEnd;

        public Settings_PreferencesViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            LoadUserSettings();
        }

        public AppSettings Settings => AppSettings.Instance;

        private void LoadUserSettings()
        {
            // Load all settings from AppSettings instance
            CardsPerSession = Settings.CardsPerSession;
            AutoPlayAudio = Settings.AutoPlayAudio;
            PreloadMultimedia = Settings.PreloadMultimedia;
            IntervalDuration = Settings.IntervalDuration;
            EnableReviewReminders = Settings.EnableReviewReminders;
            
            // ComboBox items will be set in the View's initialization
        }

        public ComboBoxItem DefaultExportFormat
        {
            get => _defaultExportFormat;
            set
            {
                _defaultExportFormat = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    string content = value.Content?.ToString();
                    if (content != null && Settings.DefaultExportFormat != content)
                    {
                        Settings.DefaultExportFormat = content;
                    }
                }
            }
        }

        public int CardsPerSession
        {
            get => Settings.CardsPerSession;
            set
            {
                if (Settings.CardsPerSession != value)
                {
                    Settings.CardsPerSession = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoPlayAudio
        {
            get => Settings.AutoPlayAudio;
            set
            {
                if (Settings.AutoPlayAudio != value)
                {
                    Settings.AutoPlayAudio = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PreloadMultimedia
        {
            get => Settings.PreloadMultimedia;
            set
            {
                if (Settings.PreloadMultimedia != value)
                {
                    Settings.PreloadMultimedia = value;
                    OnPropertyChanged();
                }
            }
        }

        public ComboBoxItem AutoPlayMode
        {
            get => _autoPlayMode;
            set
            {
                _autoPlayMode = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    string content = value.Content?.ToString();
                    if (content != null && Settings.AutoPlayMode != content)
                    {
                        Settings.AutoPlayMode = content;
                    }
                }
            }
        }

        public int IntervalDuration
        {
            get => Settings.IntervalDuration;
            set
            {
                if (Settings.IntervalDuration != value)
                {
                    Settings.IntervalDuration = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableReviewReminders
        {
            get => Settings.EnableReviewReminders;
            set
            {
                if (Settings.EnableReviewReminders != value)
                {
                    Settings.EnableReviewReminders = value;
                    OnPropertyChanged();
                }
            }
        }

        public ComboBoxItem QuietHoursStart
        {
            get => _quietHoursStart;
            set
            {
                _quietHoursStart = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    string content = value.Content?.ToString();
                    if (content != null && Settings.QuietHoursStart != content)
                    {
                        Settings.QuietHoursStart = content;
                    }
                }
            }
        }

        public ComboBoxItem QuietHoursEnd
        {
            get => _quietHoursEnd;
            set
            {
                _quietHoursEnd = value;
                OnPropertyChanged();
                
                if (value != null)
                {
                    string content = value.Content?.ToString();
                    if (content != null && Settings.QuietHoursEnd != content)
                    {
                        Settings.QuietHoursEnd = content;
                    }
                }
            }
        }
    }
}
