using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    class Settings_PreferencesViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        public Settings_PreferencesViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }
    }
}
