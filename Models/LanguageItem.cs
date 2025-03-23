using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.Models
{
    public class LanguageItem
    {
        public string Name { get; set; }
        public string Icon { get; set; }

        public LanguageItem(string name, string icon)
        {
            Name = name;
            Icon = icon;
        }
    }


}
