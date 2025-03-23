using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public partial class LearningPathViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        public LearningPathViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToCreatePath = new RelayCommand(ExecuteNavigateToCreatePath);
        }

        public ICommand NavigateToCreatePath { get; }

        private void ExecuteNavigateToCreatePath()
        {
            _navigationService.NavigateTo(new CreatePathViewModel(_navigationService));
        }
    }
}
