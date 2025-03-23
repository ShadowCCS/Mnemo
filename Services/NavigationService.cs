using MnemoProject.ViewModels;
using System;
using System.Collections.Generic;

namespace MnemoProject.Services
{
    public class NavigationService
    {
        private readonly Stack<ViewModelBase> _navigationStack = new();
        public ViewModelBase? CurrentView { get; private set; }
        public event Action? NavigationChanged;

        public void NavigateTo(ViewModelBase viewModel)
        {
            if (CurrentView != null)
            {
                _navigationStack.Push(CurrentView);
            }
            CurrentView = viewModel;
            NavigationChanged?.Invoke();
        }

        public void GoBack()
        {
            if (_navigationStack.Count > 0)
            {
                CurrentView = _navigationStack.Pop();
                NavigationChanged?.Invoke();
            }
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
