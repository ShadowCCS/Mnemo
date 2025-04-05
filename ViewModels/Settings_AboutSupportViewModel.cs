using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    class Settings_AboutSupportViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        public Settings_AboutSupportViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;

            AppVersion = VersionInfo.Version;
            _buildID = VersionInfo.BuildID;

        }

        private string _appVersion;

        private string _buildID;
        public string AppVersion
        {
            get => _appVersion + ":" + _buildID;
            set => SetProperty(ref _appVersion, value);
        }

    }
}
