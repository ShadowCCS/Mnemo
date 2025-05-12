using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Services;
using MnemoProject.Models;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using System.Linq;

namespace MnemoProject.ViewModels.Overlays
{
    public partial class ExportOverlayViewModel : ViewModelBase
    {
        private readonly ImportExportService _importExportService;
        private readonly Guid _itemId;
        private readonly ExportType _exportType;
        private readonly Window _mainWindow;

        [ObservableProperty]
        private string _outputPath;

        [ObservableProperty]
        private int _progress;

        [ObservableProperty]
        private string _exportStatus;

        [ObservableProperty]
        private bool _isExporting;

        [ObservableProperty]
        private string _selectedExportType;

        public List<string> ExportTypes { get; } = new List<string> { "Mnemo Format (.mnemo)" };

        public bool CanExport => !string.IsNullOrWhiteSpace(OutputPath) && !IsExporting;

        public ExportOverlayViewModel(Window mainWindow, ExportType exportType, Guid itemId)
        {
            _mainWindow = mainWindow;
            _exportType = exportType;
            _itemId = itemId;
            _importExportService = new ImportExportService();
            
            // Default to Documents folder
            OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MnemoExports");
            SelectedExportType = ExportTypes[0]; // Default to first option
        }

        [RelayCommand]
        private async Task BrowseAsync()
        {
            try
            {
                var folderPicker = await _mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Export Location",
                    AllowMultiple = false
                });

                var selectedFolder = folderPicker.FirstOrDefault();
                if (selectedFolder != null)
                {
                    OutputPath = selectedFolder.Path.LocalPath;
                    OnPropertyChanged(nameof(CanExport));
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Failed to open folder dialog: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ExportAsync()
        {
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                NotificationService.Warning("Please select an output location first.");
                return;
            }

            try
            {
                IsExporting = true;
                Progress = 0;
                ExportStatus = "Preparing export...";

                // Simulate progress updates
                var progressTimer = new System.Timers.Timer(100);
                progressTimer.Elapsed += (sender, e) =>
                {
                    if (Progress < 90) // Cap at 90% until done
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            Progress += 1;
                        });
                    }
                };
                progressTimer.Start();

                string result = null;
                
                ExportStatus = "Exporting data...";
                
                if (_exportType == ExportType.FlashcardDeck)
                {
                    result = await _importExportService.ExportFlashcardDeckAsync(_itemId, OutputPath);
                }
                else if (_exportType == ExportType.LearningPath)
                {
                    result = await _importExportService.ExportLearningPathAsync(_itemId, OutputPath);
                }

                progressTimer.Stop();
                progressTimer.Dispose();

                if (result != null)
                {
                    Progress = 100;
                    ExportStatus = "Export completed successfully!";
                    NotificationService.Success($"Successfully exported to {result}");
                    
                    // Close the overlay after a short delay
                    await Task.Delay(1500);
                    Close();
                }
                else
                {
                    ExportStatus = "Export failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Export failed: {ex.Message}");
                ExportStatus = "Export failed. Please try again.";
            }
            finally
            {
                IsExporting = false;
                OnPropertyChanged(nameof(CanExport));
            }
        }

        [RelayCommand]
        private void Close()
        {
            OverlayService.Instance.CloseOverlay();
        }
    }
} 