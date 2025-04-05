using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        public DashboardViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

    }
}
