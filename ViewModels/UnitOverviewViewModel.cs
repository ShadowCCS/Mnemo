using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MnemoProject.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using MnemoProject.Data;
using MnemoProject.Models;
using System.Collections.ObjectModel;

using System.IO;

namespace MnemoProject.ViewModels
{
    public partial class UnitOverviewViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private readonly DatabaseService _databaseService = new();
        public LearningPath CurrentLearningPath { get; private set; }

        string _userInput = "";


        [ObservableProperty]
        private ObservableCollection<UnitViewModel> _units;

        public UnitOverviewViewModel(NavigationService navigationService, Guid learningPathID, string userInput)
        {
            _navigationService = navigationService;
            LoadUnits(learningPathID);

            _userInput = userInput;
        }

        private async void LoadUnits(Guid learningPathID)
        {
            CurrentLearningPath = await _databaseService.GetLearningPathById(learningPathID);
            if (CurrentLearningPath != null)
            {
                var sortedUnits = CurrentLearningPath.Units.OrderBy(u => u.UnitNumber);
                Units = new ObservableCollection<UnitViewModel>(
                    sortedUnits.Select(u => new UnitViewModel(u, this)));
            }
        }

        private async void GenerateNextUnitContent(int nextUnitNumber)
        {
            var nextUnit = CurrentLearningPath.Units.FirstOrDefault(u => u.UnitNumber == nextUnitNumber);
            if (nextUnit != null && string.IsNullOrWhiteSpace(nextUnit.UnitContent))
            {
                string theoryContent = !string.IsNullOrWhiteSpace(nextUnit.TheoryContent)
                    ? nextUnit.TheoryContent
                    : $"Generate content for '{nextUnit.Title}'.";

                var aiService = new AIService(new GeminiProvider(), new AIWorker());
                await aiService.GenerateUnitContent(nextUnit.Title, theoryContent, async content =>
                {
                    nextUnit.UnitContent = string.IsNullOrWhiteSpace(content) ? "Default generated content." : content;
                    await _databaseService.UpdateUnit(nextUnit);
                });
            }
        }


            [RelayCommand]
        public void GoBack()
        {
            _navigationService.GoBack();
        }

        [RelayCommand]
        public void GoHome()
        {
            _navigationService.NavigateTo(new LearningPathViewModel(_navigationService));
        }

        public void NavigateToUnitGuide(Unit unit)
        {
            string unitContent = unit.UnitContent;


            _navigationService.NavigateTo(new UnitGuideViewModel(_navigationService, unit.Title, unitContent));
            string directoryPath = "D:\\Temp";

            string sanitizedTitle = string.Concat(unit.Title.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(directoryPath, $"{sanitizedTitle}.txt");

            File.WriteAllText(filePath, unitContent);

            GenerateNextUnitContent(unit.UnitNumber + 1);
        }
    }

    public partial class UnitViewModel : ObservableObject
    {
        private readonly Unit _unit;
        private readonly UnitOverviewViewModel _parentViewModel;

        public UnitViewModel(Unit unit, UnitOverviewViewModel parentViewModel)
        {
            _unit = unit;
            _parentViewModel = parentViewModel;

            System.Diagnostics.Debug.WriteLine($"Unit: {Title}, TheoryContent: {TheoryContent}, UnitContent: {UnitContent}, IsEnabled: {IsEnabled}");
        }

        public int UnitNumber => _unit.UnitNumber;
        public string Title => _unit.Title;
        public string TheoryContent => _unit.TheoryContent;
        public string UnitContent => _unit.UnitContent;
        public bool IsEnabled => !string.IsNullOrEmpty(_unit.UnitContent);


        [RelayCommand]
        public void OpenUnitGuide()
        {
            _parentViewModel.NavigateToUnitGuide(_unit);
        }
    }
}
