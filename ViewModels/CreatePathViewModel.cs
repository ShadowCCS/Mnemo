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
    public partial class CreatePathViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private string _learningPathText;

        public CreatePathViewModel(NavigationService navigationService)
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

        [RelayCommand]
        public void sendCommand()
        {
            _navigationService.NavigateTo(new learningPathCreateViewModel(_navigationService, LearningPathText));
        }
    }
}
