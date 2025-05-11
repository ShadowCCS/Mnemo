using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MnemoProject.Models;
using MnemoProject.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MnemoProject.ViewModels.Overlays
{
    public class FileUploadOverlayViewModel : ViewModelBase
    {
        private readonly CreatePathViewModel _parentViewModel;
        
        public ObservableCollection<UploadFileItem> SelectedFiles { get; } = new ObservableCollection<UploadFileItem>();

        public ICommand CloseCommand { get; }
        public ICommand BrowseFilesCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand UploadFilesCommand { get; }

        public FileUploadOverlayViewModel(CreatePathViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel;

            CloseCommand = new RelayCommand(ExecuteClose);
            BrowseFilesCommand = new AsyncRelayCommand(ExecuteBrowseFiles);
            RemoveFileCommand = new RelayCommand<UploadFileItem>(ExecuteRemoveFile);
            UploadFilesCommand = new AsyncRelayCommand(ExecuteUploadFiles);
        }

        private void ExecuteClose()
        {
            OverlayService.Instance.CloseOverlay();
        }

        private async Task ExecuteBrowseFiles()
        {
            // Get the main window
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
                
            if (mainWindow == null)
                return;

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Files",
                AllowMultiple = true,
                FileTypeFilter = new[] 
                { 
                    new FilePickerFileType("Document Files") 
                    { 
                        Patterns = new[] { "*.txt", "*.pdf", "*.docx" },
                        MimeTypes = new[] { "text/plain", "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
                    }
                }
            });

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    AddFileToList(file);
                }
            }
        }

        private void ExecuteRemoveFile(UploadFileItem fileItem)
        {
            if (fileItem != null)
            {
                SelectedFiles.Remove(fileItem);
            }
        }

        private async Task ExecuteUploadFiles()
        {
            if (SelectedFiles.Count == 0)
            {
                NotificationService.Warning("Please select at least one file to upload");
                return;
            }

            try
            {
                StringBuilder referenceContent = new StringBuilder();

                foreach (var file in SelectedFiles)
                {
                    string fileContent = await ExtractTextFromFile(file.FilePath);
                    referenceContent.AppendLine($"[{file.FileType}] {file.FileName}:");
                    referenceContent.AppendLine(fileContent);
                    referenceContent.AppendLine();
                }

                // Use the new method to add reference content without affecting user input
                _parentViewModel.AddReferenceContent(referenceContent.ToString());
                
                // Close the overlay
                OverlayService.Instance.CloseOverlay();
                
                // Show success notification
                NotificationService.Success($"Added {SelectedFiles.Count} file(s) as reference material");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error processing files: {ex.Message}");
            }
        }

        public void ProcessDroppedFiles(IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    
                    // Check if the file type is supported
                    if (extension == ".txt" || extension == ".pdf" || extension == ".docx")
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists && fileInfo.Length > 0)
                        {
                            var fileItem = new UploadFileItem
                            {
                                FileName = Path.GetFileName(filePath),
                                FilePath = filePath,
                                FileSize = FormatFileSize(fileInfo.Length),
                                FileType = GetFileTypeFromExtension(extension),
                                IconSource = GetIconSourceForExtension(extension)
                            };
                            
                            SelectedFiles.Add(fileItem);
                        }
                        else
                        {
                            NotificationService.Warning($"File {fileInfo.Name} could not be accessed or is empty.");
                        }
                    }
                    else
                    {
                        NotificationService.Warning($"Unsupported file type: {extension}. Only .txt, .pdf, and .docx files are supported.");
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Warning($"Error adding file: {ex.Message}");
                }
            }
        }
        
        private void AddFileToList(IStorageFile file)
        {
            try
            {
                var extension = Path.GetExtension(file.Path.LocalPath).ToLowerInvariant();
                
                // Check if the file type is supported
                if (extension == ".txt" || extension == ".pdf" || extension == ".docx")
                {
                    var fileItem = new UploadFileItem
                    {
                        FileName = file.Name,
                        FilePath = file.Path.LocalPath,
                        FileSize = "Calculating...",
                        FileType = GetFileTypeFromExtension(extension),
                        IconSource = GetIconSourceForExtension(extension)
                    };
                    
                    // Get file size asynchronously
                    Task.Run(() =>
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file.Path.LocalPath);
                            fileItem.FileSize = FormatFileSize(fileInfo.Length);
                        }
                        catch (Exception ex)
                        {
                            NotificationService.Error($"Error getting file info: {ex.Message}");
                            fileItem.FileSize = "Unknown";
                        }
                    });
                    
                    SelectedFiles.Add(fileItem);
                }
                else
                {
                    NotificationService.Warning($"Unsupported file type: {extension}. Only .txt, .pdf, and .docx files are supported.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Warning($"Error adding file: {ex.Message}");
            }
        }

        private string GetFileTypeFromExtension(string extension)
        {
            return extension switch
            {
                ".txt" => "Text File",
                ".pdf" => "PDF Document",
                ".docx" => "Word Document",
                _ => "Unknown File Type"
            };
        }
        
        private string GetIconSourceForExtension(string extension)
        {
            return extension switch
            {
                ".txt" => "/Assets/Icons/txtFileIcon.svg",
                ".pdf" => "/Assets/Icons/txtFileIcon.svg", // Using txtFileIcon for PDF as per requirements
                ".docx" => "/Assets/Icons/microsoftFileIcon.svg",
                _ => "/Assets/Icons/txtFileIcon.svg"
            };
        }
        
        private string FormatFileSize(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= scale && order < orders.Length - 1)
            {
                order++;
                size = size / scale;
            }
            
            return $"{size:0.##} {orders[order]}";
        }

        private async Task<string> ExtractTextFromFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            try
            {
                switch (extension)
                {
                    case ".txt":
                        return await File.ReadAllTextAsync(filePath);
                        
                    case ".pdf":
                        return ExtractTextFromPdf(filePath);
                        
                    case ".docx":
                        return ExtractTextFromDocx(filePath);
                        
                    default:
                        return "[Unsupported file format]";
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error extracting text: {ex.Message}");
                return $"[Error extracting text: {ex.Message}]";
            }
        }
        
        private string ExtractTextFromPdf(string filePath)
        {
            StringBuilder text = new StringBuilder();
            
            try
            {
                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    int pageCount = document.NumberOfPages;
                    text.AppendLine($"PDF with {pageCount} pages");
                    text.AppendLine();
                    
                    // Extract text from each page
                    foreach (var page in document.GetPages())
                    {
                        text.AppendLine($"--- Page {page.Number} ---");
                        text.AppendLine(page.Text);
                        text.AppendLine();
                    }
                }
                
                return text.ToString();
            }
            catch (Exception ex)
            {
                NotificationService.LogToFile($"Error extracting PDF text: {ex}");
                return $"[Error extracting PDF text: {ex.Message}]";
            }
        }
        
        private string ExtractTextFromDocx(string filePath)
        {
            StringBuilder text = new StringBuilder();
            
            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
                {
                    var body = doc.MainDocumentPart?.Document.Body;
                    
                    if (body != null)
                    {
                        text.AppendLine("DOCX Document");
                        text.AppendLine();
                        
                        // Extract text from paragraphs
                        foreach (var paragraph in body.Elements<Paragraph>())
                        {
                            var paraText = paragraph.InnerText?.Trim();
                            if (!string.IsNullOrWhiteSpace(paraText))
                            {
                                text.AppendLine(paraText);
                                text.AppendLine();
                            }
                        }
                    }
                    else
                    {
                        text.AppendLine("[Empty or unreadable document]");
                    }
                }
                
                return text.ToString();
            }
            catch (Exception ex)
            {
                NotificationService.LogToFile($"Error extracting DOCX text: {ex}");
                return $"[Error extracting DOCX text: {ex.Message}]";
            }
        }
    }

    public class UploadFileItem : ObservableObject
    {
        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private string _fileSize;
        public string FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private string _fileType;
        public string FileType
        {
            get => _fileType;
            set => SetProperty(ref _fileType, value);
        }

        private string _iconSource;
        public string IconSource
        {
            get => _iconSource;
            set => SetProperty(ref _iconSource, value);
        }
    }
} 