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
                LogService.Log.Debug($"NavigationService: Added event handler. Count: {GetHandlerCount()}");
            }
            remove 
            {
                _navigationChanged -= value;
                LogService.Log.Debug($"NavigationService: Removed event handler. Count: {GetHandlerCount()}");
            }
        }
        
        private int GetHandlerCount()
        {
            if (_navigationChanged == null) return 0;
            return _navigationChanged.GetInvocationList().Length;
        }

        public void NavigateTo(ViewModelBase viewModel)
        {
            if (viewModel == null)
            {
                LogService.Log.Error("Attempted to navigate to null view model");
                throw new ArgumentNullException(nameof(viewModel));
            }

            try
            {
                // Check if trying to navigate to the same view model type
                if (CurrentView != null && CurrentView.GetType() == viewModel.GetType())
                {
                    LogService.Log.Debug($"Skipping navigation to same view type: {viewModel.GetType().Name}");
                    return;
                }
                
                LogService.Log.Info($"Navigating to: {viewModel.GetType().Name}");
                _navigationStack.Push(viewModel);
                OnNavigationChanged();
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Navigation error: {ex.Message}");
                throw;
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
                    LogService.Log.Info($"Navigated back from: {previousView.GetType().Name} to: {CurrentView?.GetType().Name ?? "null"}");
                }
                else
                {
                    LogService.Log.Debug("Cannot go back: at root of navigation stack");
                }
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Navigation error while going back: {ex.Message}");
                throw;
            }
        }

        private void OnNavigationChanged()
        {
            try
            {
                _navigationChanged?.Invoke();
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Error in navigation changed event: {ex.Message}");
            }
        }

        public void NavigateToNested<TParent>(
            TParent parent,
            ViewModelBase nestedViewModel,
            Action<TParent, ViewModelBase> setNestedProperty)
            where TParent : ViewModelBase
        {
            if (parent == null)
            {
                LogService.Log.Error("Attempted to navigate to nested view with null parent");
                throw new ArgumentNullException(nameof(parent));
            }

            if (nestedViewModel == null)
            {
                LogService.Log.Error("Attempted to navigate to null nested view model");
                throw new ArgumentNullException(nameof(nestedViewModel));
            }

            if (setNestedProperty == null)
            {
                LogService.Log.Error("Attempted to navigate to nested view with null setter");
                throw new ArgumentNullException(nameof(setNestedProperty));
            }

            try
            {
                LogService.Log.Debug($"Navigating to nested view: {nestedViewModel.GetType().Name} in parent: {parent.GetType().Name}");
                setNestedProperty(parent, nestedViewModel);
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Error navigating to nested view: {ex.Message}");
                throw;
            }
        }
    }
}
