using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MnemoProject.Models;
using MnemoProject.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using MnemoProject.Data;
using System.Threading.Tasks;

namespace MnemoProject.ViewModels.Overlays
{
    public partial class BulkAddOverlayViewModel : ViewModelBase
    {
        private readonly CreateDeckOverlayViewModel _parentViewModel;
        private readonly MnemoContext _dbContext;
        
        [ObservableProperty]
        private string _bulkText;
        
        public BulkAddOverlayViewModel(CreateDeckOverlayViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel;
            _dbContext = new MnemoContext();
            BulkText = string.Empty;
        }
        
        [RelayCommand]
        private void Close()
        {
            OverlayService.Instance.CloseOverlay();
        }
        
        [RelayCommand]
        private void AddBulkCards()
        {
            if (string.IsNullOrWhiteSpace(BulkText))
            {
                NotificationService.Warning("Bulk text cannot be empty");
                return;
            }
            
            var lines = BulkText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int successCount = 0;
            int failureCount = 0;
            List<Deck> newCards = new List<Deck>();
            
            // Process all cards first
            foreach (var line in lines)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Try to split by comma
                var parts = line.Split(new[] { ',' }, 2);
                
                if (parts.Length != 2)
                {
                    failureCount++;
                    continue;
                }
                
                var front = parts[0].Trim();
                var back = parts[1].Trim();
                
                if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
                {
                    failureCount++;
                    continue;
                }
                
                var newCard = new Deck
                {
                    Id = Guid.NewGuid(),
                    Front = front,
                    Back = back,
                    FlashcardId = _parentViewModel.GetFlashcardId()
                };
                
                newCards.Add(newCard);
                successCount++;
            }
            
            // Add all cards at once to the parent's collection
            if (newCards.Count > 0)
            {
                // Add cards to the parent view model's collection
                foreach (var card in newCards)
                {
                    _parentViewModel.Cards.Add(card);
                }
                
                // Notify success
                NotificationService.Success($"Successfully added {successCount} cards to deck");
                
                // Clear the bulk text
                BulkText = string.Empty;
                
                // Close only the bulk add overlay, not the parent create deck overlay
                Close();
            }
            
            if (failureCount > 0)
            {
                NotificationService.Warning($"{failureCount} cards failed due to invalid format. Use 'front, back' format for each line.");
            }
            else if (successCount == 0)
            {
                NotificationService.Warning("No valid cards found. Use 'front, back' format for each line.");
            }
        }
    }
} 