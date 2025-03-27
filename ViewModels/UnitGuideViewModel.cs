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

namespace MnemoProject.ViewModels
{
    partial class UnitGuideViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;

        private string _unitTitle = "Unit Guide";

        int unitTitleMaxLength = 25;
        public string UnitTitle
        {
            get => _unitTitle;

            set
            {
                if (value.Length > unitTitleMaxLength)
                {
                    value = value.Substring(0, unitTitleMaxLength);
                    value += "...";
                }
                else
                {
                    value = value;
                }

                SetProperty(ref _unitTitle, value);
            }
        }

        private readonly MarkdownRendererService _markdownRenderer;
        private Control _renderedMarkdown;

        public Control RenderedMarkdown
        {
            get => _renderedMarkdown;
            set => SetProperty(ref _renderedMarkdown, value);
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
                    IsUnitGuideSelected = false;
                    IsFlashcardsSelected = false;
                    IsLearnModeSelected = false;
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
                    IsUnitGuideSelected = false;
                    IsQuestionsSelected = false;
                    IsLearnModeSelected = false;
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
                }
            }
        }

        public UnitGuideViewModel(NavigationService navigationService, string unitTitle, string unitGuideText)
        {
            _navigationService = navigationService;

            UnitTitle = unitTitle;

            _markdownRenderer = new MarkdownRendererService();

            RenderedMarkdown = _markdownRenderer.RenderMarkdown(unitGuideText);

            IsUnitGuideSelected = true; // Default selection
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
