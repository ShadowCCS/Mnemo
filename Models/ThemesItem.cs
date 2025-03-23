using System;

namespace MnemoProject.Models
{
    public class ThemeItem
    {
        public string Name { get; set; }
        public string Color { get; set; }

        public ThemeItem(string name, string color)
        {
            Name = name;
            Color = color;
        }
    }
}

