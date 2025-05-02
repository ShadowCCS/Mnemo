using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.Models
{
    public class LanguageItem
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string CultureCode { get; set; }
        
        /// <summary>
        /// The native name of the language (e.g., "Español" for Spanish)
        /// </summary>
        public string NativeName { get; set; }

        public LanguageItem(string name, string icon, string cultureCode, string nativeName = "")
        {
            Name = name;
            Icon = icon;
            CultureCode = cultureCode;
            
            // Special case for French
            if (cultureCode.Equals("fr", StringComparison.OrdinalIgnoreCase))
            {
                NativeName = "Français";
                System.Diagnostics.Debug.WriteLine($"LanguageItem: Set French NativeName to 'Français'");
                return;
            }
            
            // If native name is provided, use it
            // If it's empty or same as Name, try to get it from the culture
            if (string.IsNullOrEmpty(nativeName) || nativeName == name)
            {
                try
                {
                    var culture = new CultureInfo(cultureCode);
                    string cultureName = culture.NativeName;
                    
                    // Extract just the language part without the country
                    if (cultureName.Contains(" ("))
                    {
                        cultureName = cultureName.Split(" (")[0];
                    }
                    else if (cultureName.Contains(" "))
                    {
                        cultureName = cultureName.Split(' ')[0];
                    }
                    
                    NativeName = cultureName;
                    
                    // Log for debugging
                    System.Diagnostics.Debug.WriteLine($"LanguageItem: Set NativeName for {name} ({cultureCode}) to '{NativeName}'");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting native name for {cultureCode}: {ex.Message}");
                    NativeName = name;
                }
            }
            else
            {
                NativeName = nativeName;
                System.Diagnostics.Debug.WriteLine($"LanguageItem: Using provided NativeName '{nativeName}' for {name} ({cultureCode})");
            }
        }

        public CultureInfo GetCulture()
        {
            try
            {
                return new CultureInfo(CultureCode);
            }
            catch
            {
                return CultureInfo.InvariantCulture;
            }
        }
        
        public override string ToString()
        {
            return NativeName;
        }
    }
}
