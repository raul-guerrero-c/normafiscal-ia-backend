using NormaFiscalIA.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace NormaFiscalIA.Services.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly string _apiKey;
        private readonly ILogger<OpenAIService> _logger;
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.openai.com/v1/chat/completions";

        public OpenAIService(IConfiguration config, ILogger<OpenAIService> logger)
        {
            _apiKey = config["APIs:OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(_apiKey)) throw new InvalidOperationException("API Key OpenAI no configurada");
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<string> ConsultarAsync(string consulta, string systemPrompt, int maxTokens = 2000)
        {
            try
            {
                var request = new
                {
                    model = "gpt-4",
                    max_tokens = maxTokens,
                    temperature = 0.3f,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = consulta }
                    }
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, API_URL)
                {
                    Content = JsonContent.Create(request)
                };
                httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonContent);
                var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return text ?? throw new InvalidOperationException("Respuesta vacía");
            }
            catch (Exception ex) { _logger.LogError($"Error OpenAI: {ex.Message}"); throw; }
        }

        public async Task<bool> VerificarConexionAsync()
        {
            try { var res = await ConsultarAsync("Responde OK", "Asistente de prueba", 100); return res.Contains("OK"); }
            catch { return false; }
        }
    }
}
