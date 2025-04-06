using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MnemoProject.Services
{
    public class OverlayService : ObservableObject
    {
        private static readonly OverlayService _instance = new OverlayService();
        public static OverlayService Instance => _instance;

        // Event that fires when an overlay is closed
        public event EventHandler OverlayClosed;

        private UserControl _currentOverlay;
        
        public UserControl CurrentOverlay
        {
            get => _currentOverlay;
            private set => SetProperty(ref _currentOverlay, value);
        }

        private bool _isOverlayVisible;
        
        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            private set => SetProperty(ref _isOverlayVisible, value);
        }

        private OverlayService()
        {
            IsOverlayVisible = false;
        }

        public void ShowOverlay(UserControl overlay)
        {
            if (overlay == null)
            {
                NotificationService.Warning("Attempted to show a null overlay");
                return;
            }

            CurrentOverlay = overlay;
            IsOverlayVisible = true;
            NotificationService.LogToFile($"Opened overlay: {overlay.GetType().Name}");
        }

        public void CloseOverlay()
        {
            if (IsOverlayVisible)
            {
                NotificationService.LogToFile($"Closed overlay: {CurrentOverlay?.GetType().Name}");
                CurrentOverlay = null;
                IsOverlayVisible = false;
                
                // Raise the OverlayClosed event
                OverlayClosed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
} 