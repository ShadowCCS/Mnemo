using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.ViewModels
{
    public class Settings_ApperanceViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        public Settings_ApperanceViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

    }
}
