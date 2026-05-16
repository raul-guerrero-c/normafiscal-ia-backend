using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NormaFiscalIA.Services.Interfaces;
using NormaFiscalIA.Services.Services;

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeService> _logger;
    private readonly FilesApiService _filesService;
    private readonly UnifiedPromptService _promptService;  // ← AGREGAR

    public ClaudeService(
        IConfiguration config,
        ILogger<ClaudeService> logger,
        FilesApiService filesService,
        UnifiedPromptService promptService)  // ← AGREGAR
    {
        var apiKey = config["APIs:Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("API Key no configurada");

        _client = new AnthropicClient { ApiKey = apiKey, Timeout = TimeSpan.FromSeconds(60) };
        _logger = logger;
        _filesService = filesService;
        _promptService = promptService;  // ← AGREGAR

        _logger.LogInformation("✓ Claude Service inicializado");
    }

    public async Task InitializeDocumentsAsync()
    {
        try
        {
            _logger.LogInformation("Inicializando documentos...");
            var fileIds = await _filesService.UploadAllDocumentsAsync();
            _logger.LogInformation($"✓ {fileIds.Count} documentos cargados");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error inicializando: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// USAR PROMPT UNIFICADO
    /// </summary>
    public async Task<string> ConsultarAsync(string consulta, string modo = "ejecutiva", int maxTokens = 1500)
    {
        try
        {
            _logger.LogInformation($"Claude: {consulta[..Math.Min(50, consulta.Length)]}...");

            var fileIds = _filesService.GetUploadedFileIds();
            _logger.LogInformation($"Documentos: {fileIds.Count}");

            // ← USAR PROMPT UNIFICADO
            var systemPrompt = _promptService.GetPromptByMode(modo);

            var parameters = new MessageCreateParams
            {
                Model = Model.ClaudeOpus4_7,
                MaxTokens = maxTokens,
                System = systemPrompt,  // ← PROMPT UNIFICADO
                Messages = new List<MessageParam>
                {
                    new MessageParam
                    {
                        Role = "user",
                        Content = consulta
                    }
                }
            };

            var message = await _client.Messages.Create(parameters);

            if (message?.Content == null || message.Content.Count == 0)
                throw new InvalidOperationException("Respuesta vacía");

            var contenido = message.Content.FirstOrDefault()?.Json.GetProperty("text").ToString();

            if (string.IsNullOrEmpty(contenido))
                throw new InvalidOperationException("No se obtuvo texto");

            _logger.LogInformation($"✓ Respuesta: {contenido.Length} caracteres");
            return contenido;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> VerificarConexionAsync()
    {
        try
        {
            var parameters = new MessageCreateParams
            {
                Model = Model.ClaudeOpus4_6,
                MaxTokens = 50,
                System = "Eres un asistente de prueba.",
                Messages = new List<MessageParam>
                {
                    new MessageParam
                    {
                        Role = "user",
                        Content = "Responde solo con: OK"
                    }
                }
            };

            var message = await _client.Messages.Create(parameters);
            return message?.Content?.FirstOrDefault()?.Json.GetProperty("text").ToString()?.Contains("OK") ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            return false;
        }
    }
}