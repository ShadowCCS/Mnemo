using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MnemoProject.ViewModels.Overlays;
using System;
using System.Linq;

namespace MnemoProject.Views.Overlays
{
    public partial class ImportOverlay : UserControl
    {
        private Border _dropArea;

        public ImportOverlay()
        {
            InitializeComponent();
            
            // Register drag & drop handlers
            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragOverEvent, DragOver);
            AddHandler(DragDrop.DragLeaveEvent, DragLeave);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _dropArea = this.FindControl<Border>("DropArea");
            
            // Make sure the drop area is properly configured for drag & drop
            if (_dropArea != null)
            {
                _dropArea.AddHandler(DragDrop.DropEvent, Drop);
                _dropArea.AddHandler(DragDrop.DragOverEvent, DragOver);
                _dropArea.AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            }
        }

        private void DragOver(object sender, DragEventArgs e)
        {
            // Only allow if the dragged data contains files
            if (e.Data.Contains(DataFormats.FileNames))
            {
                var files = e.Data.GetFileNames();
                
                // Check if all files have .mnemo extension
                bool allMnemoFiles = files?.All(file => file.EndsWith(".mnemo", StringComparison.OrdinalIgnoreCase)) ?? false;
                
                if (allMnemoFiles)
                {
                    e.DragEffects = DragDropEffects.Copy;
                    e.Handled = true;
                    
                    // Change the appearance of the drop area to indicate it's a valid drop target
                    if (_dropArea != null)
                    {
                        _dropArea.BorderBrush = new SolidColorBrush(Color.Parse("#4CAF50"));
                        _dropArea.BorderThickness = new Thickness(3);
                        _dropArea.Background = new SolidColorBrush(Color.Parse("#323232"));
                    }
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void DragLeave(object sender, DragEventArgs e)
        {
            // Reset the appearance of the drop area
            if (_dropArea != null)
            {
                _dropArea.BorderBrush = new SolidColorBrush(Color.Parse("#333333"));
                _dropArea.BorderThickness = new Thickness(1);
                _dropArea.Background = new SolidColorBrush(Color.Parse("#262626"));
            }
            
            e.Handled = true;
        }

        private void Drop(object sender, DragEventArgs e)
        {
            // Reset the appearance of the drop area
            if (_dropArea != null)
            {
                _dropArea.BorderBrush = new SolidColorBrush(Color.Parse("#333333"));
                _dropArea.BorderThickness = new Thickness(1);
                _dropArea.Background = new SolidColorBrush(Color.Parse("#262626"));
            }

            // Check if we have file names in the dropped data
            if (e.Data.Contains(DataFormats.FileNames))
            {
                // Get the list of file names
                var files = e.Data.GetFileNames();
                
                if (files != null && files.Any())
                {
                    // Filter for .mnemo files only
                    var mnemoFiles = files.Where(file => file.EndsWith(".mnemo", StringComparison.OrdinalIgnoreCase));
                    
                    // Pass the dropped files to the ViewModel
                    if (DataContext is ImportOverlayViewModel viewModel && mnemoFiles.Any())
                    {
                        viewModel.ProcessDroppedFiles(mnemoFiles);
                        e.Handled = true;
                    }
                }
            }
        }
    }
} 