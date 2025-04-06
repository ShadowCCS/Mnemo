using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public class UnitQuestionsViewModel : ViewModelBase
    {
        private string _placeholderText = "Questions content will be displayed here.";
        public string PlaceholderText
        {
            get => _placeholderText;
            set => SetProperty(ref _placeholderText, value);
        }

        public UnitQuestionsViewModel()
        {
            // Initialize with placeholder content
        }
    }
} 