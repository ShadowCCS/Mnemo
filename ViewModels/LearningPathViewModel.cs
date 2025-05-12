using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
        private bool _isInitialized = false;
        private Task _initializationTask = null;
        private Window _mainWindow;

        [ObservableProperty]
        private bool _isLoading = true;

        public ObservableCollection<LearningPath> LearningPaths { get; set; } = new();


        public LearningPathViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToCreatePath = new RelayCommand(ExecuteNavigateToCreatePath);
            DeleteLearningPathCommand = new RelayCommand<Guid>(ExecuteDeleteLearningPath);
            OpenLearningPathCommand = new RelayCommand<Guid>(ExecuteOpenLearningPath);
            ImportLearningPathCommand = new RelayCommand(ExecuteImportLearningPath);

            // Get main window from application lifetime
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _mainWindow = desktop.MainWindow;
            }

            // Start initialization immediately but don't wait for it
            _initializationTask = Task.Run(async () => 
            {
                try 
                {
                    await _databaseService.InitializeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in background initialization: {ex.Message}");
                }
            });
            
            // Then post to UI thread to load data and update UI
            Dispatcher.UIThread.Post(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;
            
            IsLoading = true;
            try
            {
                // Wait for background initialization to complete if it hasn't already
                if (_initializationTask != null)
                {
                    await _initializationTask;
                    _initializationTask = null;
                }
                
                await LoadLearningPaths();
                _isInitialized = true;
            }
            finally
            {
                IsLoading = false;
            }
        }


        public ICommand NavigateToCreatePath { get; }
        public ICommand DeleteLearningPathCommand { get; }
        public ICommand OpenLearningPathCommand { get; }
        public ICommand ImportLearningPathCommand { get; }

        private void ExecuteNavigateToCreatePath()
        {
            System.Diagnostics.Debug.WriteLine("Before navigation");
            System.Diagnostics.Debug.WriteLine($"Current view before navigation: {_navigationService.CurrentView?.GetType().Name}");

            var createPathViewModel = new CreatePathViewModel(_navigationService);
            
            _navigationService.NavigateTo(createPathViewModel);
            
            System.Diagnostics.Debug.WriteLine($"After navigation, CurrentView is: {_navigationService.CurrentView?.GetType().Name}");
        }

        private void ExecuteImportLearningPath()
        {
            var importViewModel = new ViewModels.Overlays.ImportOverlayViewModel();
            importViewModel.ImportCompletedCallback = async () => await LoadLearningPaths();

            var importOverlay = new Views.Overlays.ImportOverlay
            {
                DataContext = importViewModel
            };

            // Register for the overlay closed event
            EventHandler handler = null;
            handler = async (s, e) =>
            {
                // Unsubscribe to avoid memory leaks
                OverlayService.Instance.OverlayClosed -= handler;
                
                // Refresh the learning paths
                await LoadLearningPaths();
            };
            
            OverlayService.Instance.OverlayClosed += handler;

            // Show the overlay and set the host window
            OverlayService.Instance.ShowOverlay(importOverlay);
            importViewModel.SetHostWindow(_mainWindow);
        }

        private async Task LoadLearningPaths()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting to load learning paths...");
                
                // Run the database query on a background thread
                var learningPaths = await Task.Run(async () => 
                {
                    try
                    {
                        return await _databaseService.GetAllLearningPaths();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in GetAllLearningPaths: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        throw;
                    }
                });
                
                System.Diagnostics.Debug.WriteLine($"Retrieved {learningPaths.Count} learning paths");
                
                // Update UI on the UI thread
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    LearningPaths.Clear();
                    foreach (var learningPath in learningPaths)
                    {
                        LearningPaths.Add(learningPath);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadLearningPaths: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                // You might want to show an error message to the user here
            }
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
