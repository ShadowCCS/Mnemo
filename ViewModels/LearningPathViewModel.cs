using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
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
        public ObservableCollection<LearningPath> LearningPaths { get; set; } = new();

        public LearningPathViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToCreatePath = new RelayCommand(ExecuteNavigateToCreatePath);
            NavigateToSkip = new RelayCommand(ExecuteNavigateToSkip);

            Task.Run(async () => await LoadLearningPaths());
        }

        public ICommand NavigateToCreatePath { get; }
        public ICommand NavigateToSkip { get; }

        private void ExecuteNavigateToCreatePath()
        {
            _navigationService.NavigateTo(new CreatePathViewModel(_navigationService));
        }

        private void ExecuteNavigateToSkip()
        {
            //_navigationService.NavigateTo(new UnitOverviewViewModel(_navigationService));
        }

        //[RelayCommand]
        //private void OpenLearningPathCommand(Guid learningPathId)
        //{
        //    _navigationService.NavigateTo(new UnitOverviewViewModel(_navigationService, learningPathId));
        //}

        public async Task LoadLearningPaths()
        {
            var paths = await _databaseService.GetAllLearningPaths();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LearningPaths.Clear();
                foreach (var path in paths)
                {
                    LearningPaths.Add(path);
                }
            });
        }
    }
}
