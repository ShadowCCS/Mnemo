using MnemoProject.ViewModels;
using System;
using System.Collections.Generic;

namespace MnemoProject.Services
{
    public class NavigationService
    {
        private readonly Stack<ViewModelBase> _navigationStack = new();
        
        public ViewModelBase? CurrentView => _navigationStack.Count > 0 ? _navigationStack.Peek() : null;
        
        private event Action? _navigationChanged;
        
        public event Action NavigationChanged
        {
            add 
            {
                _navigationChanged += value;
                NotificationService.LogToFile($"NavigationService: Added event handler. Count: {GetHandlerCount()}");
            }
            remove 
            {
                _navigationChanged -= value;
                NotificationService.LogToFile($"NavigationService: Removed event handler. Count: {GetHandlerCount()}");
            }
        }
        
        private int GetHandlerCount()
        {
            if (_navigationChanged == null) return 0;
            return _navigationChanged.GetInvocationList().Length;
        }

        public void NavigateTo(ViewModelBase viewModel)
        {
            try
            {
                // Check if trying to navigate to the same view model type
                if (CurrentView != null && CurrentView.GetType() == viewModel.GetType())
                {
                    System.Diagnostics.Debug.WriteLine($"Attempted to navigate to the same view type: {viewModel.GetType().Name}");
                    return;
                }
                
                if (viewModel == null)
                {
                    NotificationService.Error("Invalid navigation: view model is null");
                    return;
                }
                
                _navigationStack.Push(viewModel);
                OnNavigationChanged();
                NotificationService.LogToFile($"Navigated to: {viewModel.GetType().Name}");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Navigation error: {ex.Message}");
                NotificationService.LogToFile($"[ERROR] NavigateTo: {ex}");
            }
        }

        public bool CanGoBack => _navigationStack.Count > 1;

        public void GoBack()
        {
            try
            {
                if (CanGoBack)
                {
                    var previousView = _navigationStack.Peek();
                    _navigationStack.Pop();
                    OnNavigationChanged();
                    NotificationService.LogToFile($"Navigated back from: {previousView.GetType().Name} to: {CurrentView?.GetType().Name}");
                }
                else
                {
                    NotificationService.LogToFile("Cannot go back: at root of navigation stack");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Navigation error while going back: {ex.Message}");
                NotificationService.LogToFile($"[ERROR] GoBack: {ex}");
            }
        }

        private void OnNavigationChanged()
        {
            _navigationChanged?.Invoke();
        }

        public void NavigateToNested<TParent>(
            TParent parent,
            ViewModelBase nestedViewModel,
            Action<TParent, ViewModelBase> setNestedProperty)
            where TParent : ViewModelBase
        {
            setNestedProperty(parent, nestedViewModel);
        }
    }
}
