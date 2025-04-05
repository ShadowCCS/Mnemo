using System;

namespace MnemoProject.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Truncates a string to the specified maximum length and adds an ellipsis if truncated.
        /// </summary>
        /// <param name="text">The text to truncate</param>
        /// <param name="maxLength">Maximum length before truncation</param>
        /// <param name="suffix">The suffix to add when truncated (default: "...")</param>
        /// <returns>The truncated string with suffix if needed</returns>
        
        public static string Truncate(string text, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            if (text.Length <= maxLength) return text;
            
            return text.Substring(0, maxLength) + suffix;
        }
    }
} 