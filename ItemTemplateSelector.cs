using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using MnemoProject.ViewModels;
using System;

namespace MnemoProject
{
    public class ItemTemplateSelector : IDataTemplate
    {
        public Control Build(object param)
        {
            if (param is ListItemTemplate listItem)
            {
                return new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 15,
                    Children =
                    {
                        new PathIcon
                        {
                            Width = 16,
                            Height = 16,
                            [!PathIcon.DataProperty] = new Binding("ListItemIcon"),
                            Foreground = Brushes.White
                        },
                        new TextBlock
                        {
                            [!TextBlock.TextProperty] = new Binding("Label")
                        }
                    }
                };
            }
            else if (param is LabelItem labelItem)
            {
                return new ContentControl
                {
                    Content = new TextBlock
                    {
                        [!TextBlock.TextProperty] = new Binding("Label"),
                        Foreground = Brushes.White,
                        FontWeight = FontWeight.Medium,
                        IsHitTestVisible = false,
                        IsEnabled = false 
                    },
                    IsHitTestVisible = false,
                    Focusable = false,
                    IsEnabled = false,     
                    Background = Brushes.Transparent  
                };
            }

            throw new NotSupportedException();
        }

        public bool Match(object data)
        {
            return data is ListItemTemplate || data is LabelItem;
        }
    }
}
