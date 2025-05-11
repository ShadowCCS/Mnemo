using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using MnemoProject.Views.Overlays;
using MnemoProject.ViewModels.Overlays;
using System.Text.RegularExpressions;

namespace MnemoProject.ViewModels
{
    public class CreatePathViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private int _uploadedFileCount = 0;
        
        private string _userInput = "";
        public string UserInput
        {
            get => _userInput;
            set => SetProperty(ref _userInput, value);
        }
        
        private string _referenceContent = "";
        public string ReferenceContent
        {
            get => _referenceContent;
            set 
            {
                if (SetProperty(ref _referenceContent, value))
                {
                    // Update file count and notify property changes
                    UpdateFileCount();
                    OnPropertyChanged(nameof(HasReferenceFiles));
                    OnPropertyChanged(nameof(ReferenceFilesInfo));
                }
            }
        }
        
        // Indicates if there are reference files attached
        public bool HasReferenceFiles => !string.IsNullOrWhiteSpace(ReferenceContent);
        
        // Provides info about the number of reference files
        public string ReferenceFilesInfo => _uploadedFileCount > 1 
            ? $"{_uploadedFileCount} Reference Files Attached" 
            : "1 Reference File Attached";
        
        // Gets combined input for the AI (user input + reference materials)
        public string GetCombinedInput()
        {
            if (string.IsNullOrWhiteSpace(ReferenceContent))
                return UserInput;
            
            return UserInput + "\n\nAttached Reference Material:\n" + ReferenceContent;
        }

        public CreatePathViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            GenerateCommand = new RelayCommand(ExecuteGenerate);
            CancelCommand = new RelayCommand(ExecuteCancel);
            OpenFileUploadCommand = new RelayCommand(ExecuteOpenFileUpload);
            ClearReferenceContentCommand = new RelayCommand(ExecuteClearReferenceContent);
        }

        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenFileUploadCommand { get; }
        public ICommand ClearReferenceContentCommand { get; }
        
        private void ExecuteGenerate()
        {
            // Allow empty user input if reference files are attached
            if (string.IsNullOrWhiteSpace(UserInput) && !HasReferenceFiles)
            {
                NotificationService.Warning(LocalizationService.Instance.GetString("CreatePath_EmptyInputWarning", "Please enter a topic for your learning path or attach reference files"));
                return;
            }
            
            try
            {
                // Pass the combined input instead of just UserInput
                var vm = new LearningPathCreateViewModel(_navigationService, GetCombinedInput());
                _navigationService.NavigateTo(vm);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(
                    LocalizationService.Instance.GetString("CreatePath_Error", "Error generating learning path: {0}"), 
                    ex.Message);
                NotificationService.Error(errorMessage);
                NotificationService.LogToFile($"[ERROR] ExecuteGenerate: {ex}");
            }
        }
        
        private void ExecuteCancel()
        {
            _navigationService.GoBack();
        }
        
        private void ExecuteOpenFileUpload()
        {
            var fileUploadViewModel = new FileUploadOverlayViewModel(this);
            var fileUploadOverlay = new FileUploadOverlay
            {
                DataContext = fileUploadViewModel
            };
            
            OverlayService.Instance.ShowOverlay(fileUploadOverlay);
        }
        
        // Add reference material without affecting the user input
        public void AddReferenceContent(string content)
        {
            ReferenceContent += content;
        }
        
        // Clear all reference material
        private void ExecuteClearReferenceContent()
        {
            ReferenceContent = "";
            _uploadedFileCount = 0;
            OnPropertyChanged(nameof(HasReferenceFiles));
            OnPropertyChanged(nameof(ReferenceFilesInfo));
        }
        
        // Count the number of files in reference content by counting file headers
        private void UpdateFileCount()
        {
            if (string.IsNullOrWhiteSpace(ReferenceContent))
            {
                _uploadedFileCount = 0;
                return;
            }
            
            // Count file headers like "[Text File] filename.txt:"
            var matches = Regex.Matches(ReferenceContent, @"\[(Text File|PDF Document|Word Document)\]");
            _uploadedFileCount = matches.Count;
        }
    }
}
