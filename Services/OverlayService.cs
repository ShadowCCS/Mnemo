using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace MnemoProject.Services
{
    public class OverlayService : ObservableObject
    {
        private static readonly OverlayService _instance = new OverlayService();
        public static OverlayService Instance => _instance;

        // Event that fires when an overlay is closed
        public event EventHandler OverlayClosed;

        private UserControl _currentOverlay;
        private Stack<UserControl> _overlayStack;
        
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
            _overlayStack = new Stack<UserControl>();
        }

        public void ShowOverlay(UserControl overlay)
        {
            if (overlay == null)
            {
                NotificationService.Warning("Attempted to show a null overlay");
                return;
            }

            // If there's a current overlay, push it onto the stack
            if (CurrentOverlay != null)
            {
                _overlayStack.Push(CurrentOverlay);
            }

            CurrentOverlay = overlay;
            IsOverlayVisible = true;
            NotificationService.LogToFile($"Opened overlay: {overlay.GetType().Name}");
        }

        public void CloseOverlay()
        {
            if (IsOverlayVisible)
            {
                var closingOverlay = CurrentOverlay;
                NotificationService.LogToFile($"Closed overlay: {closingOverlay?.GetType().Name}");
                
                // Check if we have previous overlays to restore
                if (_overlayStack.Count > 0)
                {
                    CurrentOverlay = _overlayStack.Pop();
                    NotificationService.LogToFile($"Restored previous overlay: {CurrentOverlay?.GetType().Name}");
                }
                else
                {
                    CurrentOverlay = null;
                    IsOverlayVisible = false;
                }
                
                // Raise the OverlayClosed event
                OverlayClosed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
} 