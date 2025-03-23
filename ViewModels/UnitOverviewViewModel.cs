using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MnemoProject.ViewModels
{
    public partial class UnitOverviewViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        public UnitOverviewViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        public void GoBack()
        {
            _navigationService.GoBack();
        }

        [RelayCommand]
        public void GoHome()
        {
            _navigationService.NavigateTo(new LearningPathViewModel(_navigationService));
        }
    }
}
