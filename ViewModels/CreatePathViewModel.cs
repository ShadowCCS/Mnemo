using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MnemoProject.ViewModels
{
    public class CreatePathViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        
        private string _userInput = "";
        public string UserInput
        {
            get => _userInput;
            set => SetProperty(ref _userInput, value);
        }

        public CreatePathViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            GenerateCommand = new RelayCommand(ExecuteGenerate);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        
        private void ExecuteGenerate()
        {
            if (string.IsNullOrWhiteSpace(UserInput))
            {
                NotificationService.Warning("Please enter a topic for your learning path");
                return;
            }
            
            try
            {
                var vm = new LearningPathCreateViewModel(_navigationService, UserInput);
                _navigationService.NavigateTo(vm);
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error generating learning path: {ex.Message}");
                NotificationService.LogToFile($"[ERROR] ExecuteGenerate: {ex}");
            }
        }
        
        private void ExecuteCancel()
        {
            _navigationService.GoBack();
        }
    }
}
