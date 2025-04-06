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

namespace MnemoProject.ViewModels
{
    partial class UnitGuideViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private string _unitTitle = "Unit Guide";
        private const int UnitTitleMaxLength = 25;
        private readonly string _unitGuideText;
        private readonly MarkdownRendererService _markdownRenderer;

        // Child view models for each tab
        private UnitGuideContentViewModel _unitGuideContentViewModel;
        private UnitQuestionsViewModel _unitQuestionsViewModel;
        private UnitFlashcardsViewModel _unitFlashcardsViewModel;
        private UnitLearnModeViewModel _unitLearnModeViewModel;

        // Active view model
        private ViewModelBase _activeViewModel;

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

            // Initialize tab view models
            InitializeViewModels();

            // Set default tab
            IsUnitGuideSelected = true;
        }

        private void InitializeViewModels()
        {
            _unitGuideContentViewModel = new UnitGuideContentViewModel(_unitGuideText, _markdownRenderer);
            
            // Initialize with unit content
            System.Diagnostics.Debug.WriteLine($"Initializing UnitQuestionsViewModel with content length: {_unitGuideText?.Length ?? 0}");
            _unitQuestionsViewModel = new UnitQuestionsViewModel(_unitGuideText ?? string.Empty);
            
            // Ensure we're passing the unit content for flashcards generation
            System.Diagnostics.Debug.WriteLine($"Initializing UnitFlashcardsViewModel with content length: {_unitGuideText?.Length ?? 0}");
            _unitFlashcardsViewModel = new UnitFlashcardsViewModel(_unitGuideText ?? string.Empty);
            
            _unitLearnModeViewModel = new UnitLearnModeViewModel();
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
