using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using MnemoProject.Helpers;
using MnemoProject.Views.Overlays;
using MnemoProject.ViewModels.Overlays;
using MnemoProject.Data;
using MnemoProject.Models;
using Microsoft.EntityFrameworkCore;
using Avalonia;
using System.Windows.Input;

namespace MnemoProject.ViewModels
{
    partial class UnitGuideViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private string _unitTitle = "Unit Guide";
        private const int UnitTitleMaxLength = 25;
        private readonly string _unitGuideText;
        private readonly MarkdownRendererService _markdownRenderer;
        private readonly DatabaseService _databaseService = new();
        private LearningPath _currentLearningPath;
        private Unit _currentUnit;
        private readonly Window _mainWindow;

        // Child view models for each tab
        private UnitGuideContentViewModel _unitGuideContentViewModel;
        private UnitQuestionsViewModel _unitQuestionsViewModel;
        private UnitFlashcardsViewModel _unitFlashcardsViewModel;
        private UnitLearnModeViewModel _unitLearnModeViewModel;

        // Active view model
        private ViewModelBase _activeViewModel;
        
        // Commands
        public ICommand GoBackCommand { get; }
        public ICommand GoHomeCommand { get; }
        public ICommand ExportCommand { get; }

        public string UnitTitle
        {
            get => _unitTitle;
            set => SetProperty(ref _unitTitle, StringHelper.Truncate(value, UnitTitleMaxLength));
        }

        public ViewModelBase ActiveViewModel
        {
            get => _activeViewModel;
            set => SetProperty(ref _activeViewModel, value);
        }

        private bool _isUnitGuideSelected;
        public bool IsUnitGuideSelected
        {
            get => _isUnitGuideSelected;
            set
            {
                if (SetProperty(ref _isUnitGuideSelected, value) && value)
                {
                    IsQuestionsSelected = false;
                    IsFlashcardsSelected = false;
                    IsLearnModeSelected = false;
                    ActiveViewModel = _unitGuideContentViewModel;
                }
            }
        }

        private bool _isQuestionsSelected;
        public bool IsQuestionsSelected
        {
            get => _isQuestionsSelected;
            set
            {
                if (SetProperty(ref _isQuestionsSelected, value) && value)
                {
                    // Reset other tab selections
                    IsUnitGuideSelected = false;
                    IsFlashcardsSelected = false;
                    IsLearnModeSelected = false;
                    
                    // Set active view model
                    ActiveViewModel = _unitQuestionsViewModel;
                    
                    // Activate the view model to load questions
                    _unitQuestionsViewModel.OnActivated();
                }
            }
        }

        private bool _isFlashcardsSelected;
        public bool IsFlashcardsSelected
        {
            get => _isFlashcardsSelected;
            set
            {
                if (SetProperty(ref _isFlashcardsSelected, value) && value)
                {
                    // Reset other tab selections
                    IsUnitGuideSelected = false;
                    IsQuestionsSelected = false;
                    IsLearnModeSelected = false;
                    
                    // Set active view model
                    ActiveViewModel = _unitFlashcardsViewModel;
                    
                    // Activate the view model to load flashcards
                    _unitFlashcardsViewModel.OnActivated();
                }
            }
        }

        private bool _isLearnModeSelected;
        public bool IsLearnModeSelected
        {
            get => _isLearnModeSelected;
            set
            {
                if (SetProperty(ref _isLearnModeSelected, value) && value)
                {
                    IsUnitGuideSelected = false;
                    IsQuestionsSelected = false;
                    IsFlashcardsSelected = false;
                    ActiveViewModel = _unitLearnModeViewModel;
                    
                    // Activate the view model to load learn content (async fire-and-forget)
                    _ = _unitLearnModeViewModel.OnActivated();
                }
            }
        }
        
        public UnitGuideViewModel(NavigationService navigationService, string unitTitle, string unitGuideText)
        {
            _navigationService = navigationService;
            UnitTitle = unitTitle;
            _unitGuideText = unitGuideText;
            
            // Debug unit content
            System.Diagnostics.Debug.WriteLine($"UnitGuideViewModel constructor - Content length: {unitGuideText?.Length ?? 0}");
            if (string.IsNullOrEmpty(unitGuideText))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Unit guide text is empty in UnitGuideViewModel constructor");
            }
            
            _markdownRenderer = new MarkdownRendererService();
            
            // Get main window from application lifetime
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                _mainWindow = desktop.MainWindow;
            }

            // Initialize commands
            GoBackCommand = new RelayCommand(GoBack);
            GoHomeCommand = new RelayCommand(GoHome);
            ExportCommand = new RelayCommand(Export);

            // Fetch current learning path and unit
            _ = LoadLearningPathAndUnitAsync(unitTitle);

            // Initialize tab view models
            InitializeViewModels();

            // Set default tab
            IsUnitGuideSelected = true;
        }

        private async Task LoadLearningPathAndUnitAsync(string unitTitle)
        {
            try
            {
                using (var context = new MnemoContext())
                {
                    // Find the unit with the matching title
                    var unit = await context.Units
                        .Include(u => u.LearningPath)
                        .FirstOrDefaultAsync(u => u.Title == unitTitle);

                    if (unit != null)
                    {
                        _currentUnit = unit;
                        _currentLearningPath = unit.LearningPath;
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to load learning path data: {ex.Message}");
            }
        }

        private void InitializeViewModels()
        {
            _unitGuideContentViewModel = new UnitGuideContentViewModel(_unitGuideText, _markdownRenderer);
            _unitQuestionsViewModel = new UnitQuestionsViewModel(_unitGuideText);
            _unitFlashcardsViewModel = new UnitFlashcardsViewModel(_unitGuideText);
            _unitLearnModeViewModel = new UnitLearnModeViewModel(_unitGuideText, _navigationService);
        }

        private void GoBack()
        {
            _navigationService.GoBack();
        }

        private void GoHome()
        {
            _navigationService.NavigateTo(new LearningPathViewModel(_navigationService));
        }

        private void Export()
        {
            if (_currentLearningPath != null && _currentUnit != null)
            {
                var exportOverlay = new Views.Overlays.ExportOverlay
                {
                    DataContext = new ViewModels.Overlays.ExportOverlayViewModel(_mainWindow, ExportType.LearningPath, _currentLearningPath.Id)
                };
                
                OverlayService.Instance.ShowOverlay(exportOverlay);
            }
            else
            {
                NotificationService.Warning("Unable to export: Learning path information not loaded.");
            }
        }
    }
}
