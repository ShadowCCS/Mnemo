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
using MnemoProject.Helpers;

namespace MnemoProject.ViewModels
{
    public partial class UnitOverviewViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private readonly DatabaseService _databaseService = new();
        public LearningPath CurrentLearningPath { get; private set; }

        [ObservableProperty]
        private ObservableCollection<UnitViewModel> _units;

        public UnitOverviewViewModel(NavigationService navigationService, Guid learningPathID)
        {
            _navigationService = navigationService;
            LoadUnits(learningPathID);

        }

        private async void LoadUnits(Guid learningPathID)
        {
            CurrentLearningPath = await _databaseService.GetLearningPathById(learningPathID);
            if (CurrentLearningPath != null)
            {
                var sortedUnits = CurrentLearningPath.Units.OrderBy(u => u.UnitNumber).ToList();
                
                // Check if any unit after the first one already has content
                // This can happen if the user has already generated content for some units
                for (int i = 1; i < sortedUnits.Count; i++)
                {
                    var unit = sortedUnits[i];
                    var prevUnit = sortedUnits[i - 1];
                    
                    // If current unit has empty content but previous unit has content,
                    // it means we need to generate content for this unit
                    if ((string.IsNullOrWhiteSpace(unit.UnitContent) || 
                         unit.UnitContent == "Content will be generated when you open this unit.") && 
                        !string.IsNullOrWhiteSpace(prevUnit.UnitContent) &&
                        prevUnit.UnitContent != "Content will be generated when you open this unit." &&
                        prevUnit.UnitContent != "Generating content...")
                    {
                        // If the previous unit already has content, we should update this unit's content placeholder
                        unit.UnitContent = "Content will be generated when you open this unit.";
                        await _databaseService.UpdateUnit(unit);
                    }
                }
                
                Units = new ObservableCollection<UnitViewModel>(
                    sortedUnits.Select(u => new UnitViewModel(u, this)));
            }
        }

        private async void GenerateNextUnitContent(int nextUnitNumber)
        {
            var nextUnit = CurrentLearningPath.Units.FirstOrDefault(u => u.UnitNumber == nextUnitNumber);
            if (nextUnit == null) return; // No next unit exists
            
            // Find the ViewModel for the next unit to update its UI state
            var nextUnitViewModel = Units.FirstOrDefault(vm => vm.UnitNumber == nextUnitNumber);
            if (nextUnitViewModel == null) return;
            
            // Only generate if content is empty or contains the placeholder text
            bool needsGeneration = string.IsNullOrWhiteSpace(nextUnit.UnitContent) || 
                                   // Do not remove the content in this line
                                   nextUnit.UnitContent == "Content will be generated when you open this unit.";
                                   
            if (needsGeneration)
            {
                // Update UI to show generation is in progress
                if (nextUnitViewModel is UnitViewModel unitVM)
                {
                    unitVM.IsGeneratingNextUnit = true;
                }
                
                NotificationService.Info($"Generating content for Unit {nextUnitNumber}: {nextUnit.Title}");
                
                string theoryContent = !string.IsNullOrWhiteSpace(nextUnit.TheoryContent)
                    ? nextUnit.TheoryContent
                    : $"Generate content for '{nextUnit.Title}'.";

                var aiService = new AIService(new GeminiProvider(), new AIWorker());
                await aiService.GenerateUnitContent(nextUnit.Title, theoryContent, async content =>
                {
                    nextUnit.UnitContent = string.IsNullOrWhiteSpace(content) ? "Default generated content." : content;
                    await _databaseService.UpdateUnit(nextUnit);
                    
                    // Update UI after content is generated
                    if (nextUnitViewModel is UnitViewModel unitVM)
                    {
                        unitVM.IsGeneratingNextUnit = false;
                    }
                    
                    // Refresh the units collection to update the UI
                    var index = Units.IndexOf(nextUnitViewModel);
                    if (index >= 0)
                    {
                        // Enable the unit now that content is generated
                        var updatedViewModel = new UnitViewModel(nextUnit, this) { IsManuallyEnabled = true };
                        Units[index] = updatedViewModel;
                    }
                    
                    NotificationService.Success($"Unit {nextUnitNumber} content generated successfully!");
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
            
            // Optionally save content to a file
            string directoryPath = "D:\\Temp";
            Directory.CreateDirectory(directoryPath); // Ensure directory exists

            string sanitizedTitle = string.Concat(unit.Title.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(directoryPath, $"{sanitizedTitle}.txt");

            File.WriteAllText(filePath, unitContent);

            // After opening the current unit, start generating the next unit if needed
            var nextUnitNumber = unit.UnitNumber + 1;
            GenerateNextUnitContent(nextUnitNumber);
        }
    }

    public partial class UnitViewModel : ObservableObject
    {
        private readonly Unit _unit;
        private readonly UnitOverviewViewModel _parentViewModel;

        [ObservableProperty]
        private bool _isGeneratingNextUnit;
        
        [ObservableProperty]
        private bool _isManuallyEnabled;

        public UnitViewModel(Unit unit, UnitOverviewViewModel parentViewModel)
        {
            _unit = unit;
            _parentViewModel = parentViewModel;

            // If the unit already has content (not Unit 1), it should be enabled
            if (unit.UnitNumber > 1 && !string.IsNullOrWhiteSpace(unit.UnitContent) && 
                unit.UnitContent != "Content will be generated when you open this unit." &&
                unit.UnitContent != "Generating content...")
            {
                _isManuallyEnabled = true;
            }

            System.Diagnostics.Debug.WriteLine($"Unit: {Title}, TheoryContent: {TheoryContent}, UnitContent: {UnitContent}, IsEnabled: {IsEnabled}");
        }

        public int UnitNumber => _unit.UnitNumber;
        public string TheoryContent => _unit.TheoryContent;
        public string UnitContent => _unit.UnitContent;
        
        public string Title => _unit.Title;

        // Unit is enabled if it's Unit 1 or if it has been manually enabled
        public bool IsEnabled => UnitNumber == 1 || IsManuallyEnabled;

        [RelayCommand]
        public void OpenUnitGuide()
        {
            _parentViewModel.NavigateToUnitGuide(_unit);
        }
    }
}
