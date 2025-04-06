using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using MnemoProject.Models;
using MnemoProject.Services;
using System;

namespace MnemoProject
{
    public class WidgetTemplateSelector : IDataTemplate
    {
        public Control Build(object param)
        {
            if (param is Widget widget)
            {
                return WidgetFactory.CreateWidgetControl(widget);
            }

            if (param is string str && str == "NewWidgetButton")
            {
                return WidgetFactory.CreateNewWidgetButton();
            }

            throw new NotSupportedException($"Unsupported widget parameter type: {param?.GetType().Name ?? "null"}");
        }

        public bool Match(object data)
        {
            return data is Widget || (data is string str && str == "NewWidgetButton");
        }
    }
} 