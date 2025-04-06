using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public class UnitLearnModeViewModel : ViewModelBase
    {
        private string _placeholderText = "Learn Mode content will be displayed here.";
        public string PlaceholderText
        {
            get => _placeholderText;
            set => SetProperty(ref _placeholderText, value);
        }

        public UnitLearnModeViewModel()
        {
            // Initialize with placeholder content
        }
    }
} 