using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    class Settings_DataStorageViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        public Settings_DataStorageViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }
    }
}
