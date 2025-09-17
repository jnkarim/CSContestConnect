using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CSContestConnect.Web.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string? _model;

        public GeminiService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _apiKey = config["GeminiSettings:ApiKey"];
            _model = config["GeminiSettings:Model"];

            // Optional: throw if config values are missing
            if (string.IsNullOrEmpty(_apiKey))
                throw new System.ArgumentException("GeminiSettings:ApiKey is missing in configuration.");
            if (string.IsNullOrEmpty(_model))
                throw new System.ArgumentException("GeminiSettings:Model is missing in configuration.");
        }

        public async Task<string> AskGeminiAsync(string prompt)
        {
            var request = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}",
                content
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(result);

            return doc.RootElement
                      .GetProperty("candidates")[0]
                      .GetProperty("content")
                      .GetProperty("parts")[0]
                      .GetProperty("text")
                      .GetString() ?? string.Empty; // safe fallback
        }
    }
}
