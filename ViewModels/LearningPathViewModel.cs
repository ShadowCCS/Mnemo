using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Data;
using MnemoProject.Models;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public partial class LearningPathViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private readonly DatabaseService _databaseService = new();

        [ObservableProperty]
        private bool _isLoading = true;

        public ObservableCollection<LearningPath> LearningPaths { get; set; } = new();


        public LearningPathViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToCreatePath = new RelayCommand(ExecuteNavigateToCreatePath);
            DeleteLearningPathCommand = new RelayCommand<Guid>(ExecuteDeleteLearningPath);
            OpenLearningPathCommand = new RelayCommand<Guid>(ExecuteOpenLearningPath);

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadLearningPaths();
        }


        public ICommand NavigateToCreatePath { get; }
        public ICommand DeleteLearningPathCommand { get; }
        public ICommand OpenLearningPathCommand { get; }

        private void ExecuteNavigateToCreatePath()
        {
            System.Diagnostics.Debug.WriteLine("Before navigation");
            System.Diagnostics.Debug.WriteLine($"Current view before navigation: {_navigationService.CurrentView?.GetType().Name}");

            var createPathViewModel = new CreatePathViewModel(_navigationService);
            
            _navigationService.NavigateTo(createPathViewModel);
            
            System.Diagnostics.Debug.WriteLine($"After navigation, CurrentView is: {_navigationService.CurrentView?.GetType().Name}");
        }

        private async Task LoadLearningPaths()
        {
            IsLoading = true;
            var learningPaths = await Task.Run(() => _databaseService.GetAllLearningPaths());
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                LearningPaths.Clear();
                foreach (var learningPath in learningPaths)
                {
                    LearningPaths.Add(learningPath);
                }
                IsLoading = false;
            });
        }

        private async void ExecuteDeleteLearningPath(Guid learningPathId)
        {
            await _databaseService.DeleteLearningPath(learningPathId);
            var learningPath = LearningPaths.FirstOrDefault(lp => lp.Id == learningPathId);
            if (learningPath != null)
            {
                LearningPaths.Remove(learningPath);
            }
        }

        private void ExecuteOpenLearningPath(Guid learningPathId)
        {
            _navigationService.NavigateTo(new UnitOverviewViewModel(_navigationService, learningPathId));
        }
    }
}
