using System;
using Avalonia.Markup.Xaml;
using MnemoProject.Services;

namespace MnemoProject.Helpers
{
    public class LocalizationExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public object[] FormatParameters { get; set; } = Array.Empty<object>();

        public LocalizationExtension(string key)
        {
            Key = key;
        }

        public LocalizationExtension(string key, object parameter1)
        {
            Key = key;
            FormatParameters = new object[] { parameter1 };
        }

        public LocalizationExtension(string key, object parameter1, object parameter2)
        {
            Key = key;
            FormatParameters = new object[] { parameter1, parameter2 };
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            string value = LocalizationService.Instance.GetString(Key, DefaultValue);
            
            if (FormatParameters.Length > 0)
            {
                try
                {
                    return string.Format(value, FormatParameters);
                }
                catch (FormatException)
                {
                    return value;
                }
            }
            
            return value;
        }
    }
} 