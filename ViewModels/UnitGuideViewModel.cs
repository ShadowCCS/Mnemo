using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.ViewModels
{
    partial class UnitGuideViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        private string _unitTitle = "Unit Guide";

        private string _unitGuideText = "";

        public string UnitTitle
        {
            get => _unitTitle;
            set => SetProperty(ref _unitTitle, value);
        }

        public string UnitGuideText
        {
            get => _unitGuideText;
            set => SetProperty(ref _unitGuideText, value);
        }

        public UnitGuideViewModel(NavigationService navigationService, string unitTitle, string unitGuideText)
        {
            _navigationService = navigationService;

            UnitTitle = unitTitle;
            UnitGuideText = unitGuideText;
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
