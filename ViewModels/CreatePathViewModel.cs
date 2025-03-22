using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.ViewModels
{
    public partial class CreatePathViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        public CreatePathViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        public void GoBack()
        {
            _navigationService.GoBack();
        }
    }
}
