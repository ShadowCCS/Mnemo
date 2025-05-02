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
    private DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly TimeSpan _minRequestInterval = TimeSpan.FromSeconds(1); // Minimum 1 second between requests
    private static readonly int _maxRetries = 3;
    
    public GeminiProvider()
    {
    }

    public async Task<string> GenerateTextAsync(string prompt)
    {
        // Apply throttling to prevent too many requests
        await ThrottleRequestsAsync();
        
        int retryCount = 0;
        while (true)
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

                // Track when we make the request
                _lastRequestTime = DateTime.Now;
                
                System.Diagnostics.Debug.WriteLine($"Sending request to Gemini API (attempt {retryCount + 1})");
                var response = await _httpClient.PostAsync(url, requestContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API error: {response.StatusCode} - {responseBody}");
                    
                    // If we hit rate limiting (429) or service unavailable (503), retry with backoff
                    if ((response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                         response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) && 
                        retryCount < _maxRetries)
                    {
                        retryCount++;
                        int delayMs = (int)Math.Pow(2, retryCount) * 1000; // Exponential backoff
                        System.Diagnostics.Debug.WriteLine($"Rate limited. Retrying in {delayMs}ms (attempt {retryCount}/{_maxRetries})");
                        await Task.Delay(delayMs);
                        continue;
                    }
                    
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
                System.Diagnostics.Debug.WriteLine($"Exception in GenerateTextAsync: {ex.Message}");
                
                // Retry on network errors
                if (ex is HttpRequestException && retryCount < _maxRetries)
                {
                    retryCount++;
                    int delayMs = (int)Math.Pow(2, retryCount) * 1000; // Exponential backoff
                    System.Diagnostics.Debug.WriteLine($"Network error. Retrying in {delayMs}ms (attempt {retryCount}/{_maxRetries})");
                    await Task.Delay(delayMs);
                    continue;
                }
                
                return $"Error: {ex.Message}";
            }
        }
    }
    
    private async Task ThrottleRequestsAsync()
    {
        // Calculate time since last request
        var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
        
        // If we've made a request too recently, delay until the minimum interval has passed
        if (timeSinceLastRequest < _minRequestInterval)
        {
            var delayTime = _minRequestInterval - timeSinceLastRequest;
            System.Diagnostics.Debug.WriteLine($"Throttling API request. Waiting {delayTime.TotalMilliseconds}ms before sending next request");
            await Task.Delay(delayTime);
        }
    }
}
