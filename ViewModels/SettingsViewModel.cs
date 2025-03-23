using CommunityToolkit.Mvvm.Input;

namespace MnemoProject.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private ViewModelBase _currentSettingsPage;
        private int _selectedIndex;

        public ViewModelBase CurrentSettingsPage
        {
            get => _currentSettingsPage;
            set
            {
                _currentSettingsPage = value;
                OnPropertyChanged(nameof(CurrentSettingsPage));
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
                UpdateCurrentSettingsPage();
            }
        }

        public SettingsViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToAppearance();
        }


        private void UpdateCurrentSettingsPage()
        {
            switch (_selectedIndex)
            {
                case 0:
                    NavigateToAppearance();
                    break;
                case 1:
                    NavigateToPreferences();
                    break;
                case 2:
                    NavigateToDataStorage();
                    break;
                case 3:
                    NavigateToAboutSupport();
                    break;
                case 4:
                    NavigateToExperimental();
                    break;
            }
        }

        public void NavigateToAppearance()
        {
            var appearanceViewModel = new Settings_ApperanceViewModel(_navigationService);
            CurrentSettingsPage = appearanceViewModel;
        }

        public void NavigateToPreferences()
        {
            var preferencesViewModel = new Settings_PreferencesViewModel(_navigationService);
            CurrentSettingsPage = preferencesViewModel;
        }

        public void NavigateToDataStorage()
        {
            var dataStorageViewModel = new Settings_DataStorageViewModel(_navigationService);
            CurrentSettingsPage = dataStorageViewModel;
        }

        public void NavigateToAboutSupport()
        {
            var aboutSupportViewModel = new Settings_AboutSupportViewModel(_navigationService);
            CurrentSettingsPage = aboutSupportViewModel;
        }

        public void NavigateToExperimental()
        {
            var experimentalViewModel = new Settings_ExperimentalViewModel(_navigationService);
            CurrentSettingsPage = experimentalViewModel;
        }
    }
}
