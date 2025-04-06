using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public class Settings_ExperimentalViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private bool _aiEnabled;

        public Settings_ExperimentalViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public bool AIEnabled
        {
            get => _aiEnabled;
            set
            {
                if (_aiEnabled != value)
                {
                    _aiEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
