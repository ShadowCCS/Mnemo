using MnemoProject.Helpers;
using MnemoProject.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GeminiProvider : IModelProvider
{
    private static readonly HttpClient _httpClient = new(); 

    private readonly string _apiKey = ApiKeyManager.GeminiApiKey;

    public GeminiProvider()
    {
    }

    public async Task<string> GenerateTextAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                model = "gemini-2.0-flash",
                contents = new[]
                {
                new { role = "user", parts = new[] { new { text = prompt } } }
            }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {responseBody}");
                return $"Error: API request failed with status code {response.StatusCode}";
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (jsonResponse.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];

                if (firstCandidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    if (parts[0].TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString() ?? "Error: No content received";
                    }
                }
            }

            return "Error: Unexpected API response format";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }


}
