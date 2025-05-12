using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MnemoProject.ViewModels.Overlays
{
    public class ImportFileItem
    {
        public string FilePath { get; set; }
        public string FileName { get => Path.GetFileName(FilePath); }
        public string FileType { get; set; }
    }
    
    public partial class ImportOverlayViewModel : ViewModelBase
    {
        private readonly ImportExportService _importService;
        private ObservableCollection<ImportFileItem> _selectedFiles;
        private string _importStatus;
        private bool _isImporting;
        private Window _hostWindow;
        
        public ImportOverlayViewModel()
        {
            _importService = new ImportExportService();
            _selectedFiles = new ObservableCollection<ImportFileItem>();
        }
        
        public ObservableCollection<ImportFileItem> SelectedFiles
        {
            get => _selectedFiles;
            set => SetProperty(ref _selectedFiles, value);
        }
        
        public string ImportStatus
        {
            get => _importStatus;
            set => SetProperty(ref _importStatus, value);
        }
        
        public bool IsImporting
        {
            get => _isImporting;
            set 
            { 
                SetProperty(ref _isImporting, value);
                OnPropertyChanged(nameof(CanImport));
            }
        }
        
        public bool CanImport => SelectedFiles.Count > 0 && !IsImporting;
        
        public void SetHostWindow(Window window)
        {
            _hostWindow = window;
        }
        
        [RelayCommand]
        private void CloseOverlay()
        {
            // Use OverlayService to properly close the overlay
            OverlayService.Instance.CloseOverlay();
        }
        
        [RelayCommand]
        private async Task BrowseFiles()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Title = "Select .mnemo Files",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Mnemo Files", Extensions = new List<string> { "mnemo" } }
                }
            };
            
            var result = await dialog.ShowAsync(_hostWindow);
            
            if (result != null && result.Any())
            {
                ProcessDroppedFiles(result);
            }
        }
        
        public void ProcessDroppedFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                // Verify it's a .mnemo file
                if (!file.EndsWith(".mnemo", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                // Check if file already exists in the list
                if (SelectedFiles.Any(f => f.FilePath == file))
                    continue;
                
                // Try to determine file type (flashcard or learning path)
                var fileType = DetermineFileType(file);
                
                SelectedFiles.Add(new ImportFileItem
                {
                    FilePath = file,
                    FileType = fileType
                });
            }
            
            OnPropertyChanged(nameof(CanImport));
        }
        
        private string DetermineFileType(string filePath)
        {
            try
            {
                // Read first part of file to determine type
                string content = File.ReadAllText(filePath);
                
                if (content.Contains("\"type\""))
                {
                    if (content.Contains("\"type\": \"flashcarddeck\"") || content.Contains("\"type\":\"flashcarddeck\""))
                        return "Flashcards";
                        
                    if (content.Contains("\"type\": \"learningpath\"") || content.Contains("\"type\":\"learningpath\""))
                        return "Learning Path";
                }
                
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        
        [RelayCommand]
        private void RemoveFile(ImportFileItem file)
        {
            SelectedFiles.Remove(file);
            OnPropertyChanged(nameof(CanImport));
        }
        
        [RelayCommand]
        private async Task ImportFiles()
        {
            if (SelectedFiles.Count == 0)
                return;
            
            IsImporting = true;
            ImportStatus = "Importing files...";
            
            var anySuccess = false;
            var errorCount = 0;
            
            foreach (var file in SelectedFiles.ToList())
            {
                try
                {
                    ImportStatus = $"Importing {file.FileName}...";
                    
                    var result = await _importService.ImportFileAsync(file.FilePath);
                    
                    if (result)
                    {
                        anySuccess = true;
                        SelectedFiles.Remove(file);
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Error($"Error importing {file.FileName}: {ex.Message}");
                    errorCount++;
                }
            }
            
            IsImporting = false;
            
            if (anySuccess)
            {
                // Signal completion and close if all successful
                if (errorCount == 0)
                {
                    ImportCompletedCallback?.Invoke();
                    OverlayService.Instance.CloseOverlay();
                }
                else
                {
                    ImportStatus = $"Imported successfully with {errorCount} errors";
                    ImportCompletedCallback?.Invoke();
                }
            }
            else
            {
                ImportStatus = "Import failed";
            }
        }
        
        public Action ImportCompletedCallback { get; set; }
    }
} 