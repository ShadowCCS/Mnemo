using Avalonia.Controls;
using MnemoProject.Services;

namespace MnemoProject.ViewModels
{
    public class UnitGuideContentViewModel : ViewModelBase
    {
        public Control RenderedMarkdown { get; }

        public UnitGuideContentViewModel(string markdown, MarkdownRendererService renderer)
        {
            RenderedMarkdown = renderer.RenderMarkdown(markdown);
        }
    }
} 