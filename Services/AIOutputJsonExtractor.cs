using System;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace MnemoProject.Services
{
    public static class AIOutputJsonExtractor
    {
        public static string ExtractJson(string aiOutput)
        {
            if (string.IsNullOrEmpty(aiOutput))
            {
                return string.Empty;
            }

            // Strategy 1: Look for content between triple backticks with optional json tag
            var tripleBackticksRegex = new Regex(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Multiline);
            var match = tripleBackticksRegex.Match(aiOutput);
            if (match.Success)
            {
                string extracted = match.Groups[1].Value.Trim();
                if (IsValidJson(extracted))
                {
                    return extracted;
                }
            }

            // Strategy 2: Look for content between triple quotes with optional json tag
            var tripleQuotesRegex = new Regex(@"'''(?:json)?\s*([\s\S]*?)\s*'''", RegexOptions.Multiline);
            match = tripleQuotesRegex.Match(aiOutput);
            if (match.Success)
            {
                string extracted = match.Groups[1].Value.Trim();
                if (IsValidJson(extracted))
                {
                    return extracted;
                }
            }

            // Strategy 3: Look for content between single backticks with optional json tag
            var singleBackticksRegex = new Regex(@"`(?:json)?\s*([\s\S]*?)\s*`", RegexOptions.Multiline);
            match = singleBackticksRegex.Match(aiOutput);
            if (match.Success)
            {
                string extracted = match.Groups[1].Value.Trim();
                if (IsValidJson(extracted))
                {
                    return extracted;
                }
            }

            // Strategy 4: Look for content between double quotes with optional json tag
            var doubleQuotesRegex = new Regex(@"""(?:json)?\s*([\s\S]*?)\s*""", RegexOptions.Multiline);
            match = doubleQuotesRegex.Match(aiOutput);
            if (match.Success)
            {
                string extracted = match.Groups[1].Value.Trim();
                if (IsValidJson(extracted))
                {
                    return extracted;
                }
            }

            // Strategy 5: Try to find a complete JSON object or array
            var jsonPattern = new Regex(@"(\{(?:[^{}]|(?<Open>\{)|(?<-Open>\}))*(?(Open)(?!))\}|\[(?:[^\[\]]|(?<Open>\[)|(?<-Open>\]))*(?(Open)(?!))\])", RegexOptions.Multiline);
            var matches = jsonPattern.Matches(aiOutput);

            foreach (Match jsonMatch in matches)
            {
                string potentialJson = jsonMatch.Value.Trim();
                if (IsValidJson(potentialJson))
                {
                    return potentialJson;
                }
            }

            // If all else fails, check if the entire input is valid JSON
            if (IsValidJson(aiOutput))
            {
                return aiOutput;
            }

            // Nothing worked, return empty string to indicate failure
            return string.Empty;
        }

        private static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            json = json.Trim();
            if ((json.StartsWith("{") && json.EndsWith("}")) ||
                (json.StartsWith("[") && json.EndsWith("]")))
            {
                try
                {
                    JsonDocument.Parse(json);
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }
            }

            return false;
        }
    }
}