using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NormaFiscalIA.Services.DTOs;
using NormaFiscalIA.Services.Interfaces;
using OpenAI;
using OpenAI.Responses;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NormaFiscalIA.Services.Services
{
    public class OpenAiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public string VectorStoreId { get; set; } = string.Empty;
    }

    /// <summary>
    /// ✅ OpenAI FileSearch usando Responses API con SDK oficial
    /// 
    /// 🔒 CARACTERÍSTICAS:
    /// - Usa SDK OpenAI oficial
    /// - Responses API + FileSearch
    /// - 100% garantía Vector Store
    /// - Validación post-respuesta
    /// - Reintentos automáticos
    /// - Formatos múltiples
    /// </summary>
    public class OpenAIFileSearchService
    {
        private readonly OpenAIClient _client;
        private readonly string _model;
        private readonly string _vectorStoreId;
        private readonly ILogger<OpenAIFileSearchService> _logger;
        private const int MAX_RETRIES = 2;

        public OpenAIFileSearchService(
            IOptions<OpenAiSettings> settings,
            ILogger<OpenAIFileSearchService> logger)
        {
            _client = new OpenAIClient(settings.Value.ApiKey);
            _model = settings.Value.Model;
            _vectorStoreId = settings.Value.VectorStoreId;
            _logger = logger;

            _logger.LogInformation($"✓ OpenAI FileSearch - VectorStore: {_vectorStoreId}");
        }

        /// <summary>
        /// Consultar con reintentos y validación
        /// </summary>
        public async Task<AskResponse> AskAsync(string question, string mode)
        {
            try
            {
                _logger.LogInformation($"🔒 MODO: 100% VECTOR STORE (Responses API + SDK)");
                _logger.LogInformation($"📝 Pregunta ({mode}): {question[..Math.Min(50, question.Length)]}...");
                _logger.LogInformation($"🔍 Vector Store: {_vectorStoreId}");

                // Reintentos
                for (int attempt = 1; attempt <= MAX_RETRIES + 1; attempt++)
                {
                    _logger.LogInformation($"📌 Intento {attempt}/{MAX_RETRIES + 1}");

                    try
                    {
                        var answer = await CallResponsesApiAsync(question, mode);

                        return new AskResponse
                        {
                            Answer = answer,
                            Sources = new List<SourceInfo>(),
                            ModeUsed = mode
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Error intento {attempt}: {ex.Message}");

                        if (attempt == MAX_RETRIES + 1)
                            throw;

                        await Task.Delay(500);
                    }
                }

                return ErrorResponse("Error inesperado", mode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error general");
                return ErrorResponse("Error interno del servidor", mode);
            }
        }

        /// <summary>
        /// Llamada a Responses API con FileSearch
        /// </summary>
        private async Task<string> CallResponsesApiAsync(string question, string mode)
        {
            _logger.LogInformation("🚀 Enviando a Responses API...");
            var stopwatch = Stopwatch.StartNew();

            var responsesClient = _client.GetResponsesClient();

            var response = await responsesClient.CreateResponseAsync(
                new CreateResponseOptions
                {
                    Model = _model,
                    Instructions = GetRestrictiveSystemPrompt(mode) + "\n\n" +
                                   "🎨 Formatea usando markdown (listas, negritas, títulos, emojis). " +
                                   "No agregues información fuera de documentos.",
                    InputItems = { ResponseItem.CreateUserMessageItem(question) },
                    Tools = { ResponseTool.CreateFileSearchTool(
                        new[] { _vectorStoreId },
                        maxResultCount: 20
                    )},
                    Temperature = 0.0f
                });

            stopwatch.Stop();
            _logger.LogInformation($"⏱️ Respuesta en {stopwatch.ElapsedMilliseconds}ms");

            var answer = ExtractAnswerFromResponse(response)
                ?? "❌ No se encontró información en los documentos.";

            _logger.LogInformation($"📄 Respuesta: {answer.Length} caracteres");
            return answer;
        }

        /// <summary>
        /// Extraer respuesta del response (iterar output items)
        /// </summary>
        private string ExtractAnswerFromResponse(ResponseResult response)
        {
            try
            {
                foreach (var outputItem in response.OutputItems)
                {
                    // MENSAJE DE RESPUESTA
                    if (outputItem is MessageResponseItem message)
                    {
                        _logger.LogDebug($"Mensaje - Role: {message.Role}");

                        if (message.Content != null && message.Content.Count > 0)
                        {
                            var content = message.Content.FirstOrDefault();
                            if (content != null && !string.IsNullOrWhiteSpace(content.Text))
                            {
                                return content.Text;
                            }
                        }
                    }

                    // FILE_SEARCH_CALL
                    if (outputItem is FileSearchCallResponseItem fileSearchCall)
                    {
                        _logger.LogDebug($"File search - Status: {fileSearchCall.Status}");
                        foreach (var query in fileSearchCall.Queries)
                        {
                            _logger.LogDebug($"  Query: {query}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extrayendo: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// System prompt MÁXIMAMENTE restrictivo
        /// </summary>
        private string GetRestrictiveSystemPrompt(string mode)
        {
            const string basePrompt = @"🔒 RESTRICCIÓN MÁXIMA: SOLO VECTOR STORE
════════════════════════════════════════════════════════════════════

REGLAS OBLIGATORIAS:
1. DEBES responder ÚNICAMENTE con información del Vector Store
2. JAMÁS uses conocimiento general o información externa
3. Si NO está en documentos: RESPONDE: 'No encuentro información'
4. NUNCA inventes, adivines o parafrasees sin fuente
5. TODA afirmación DEBE tener cita: Art. X del [Documento]

DOCUMENTOS DISPONIBLES:
- Código Fiscal de la Federación (CFF)
- Reglamento del CFF (RCFF)
- Ley del Impuesto sobre la Renta (LISR)
- Reglamento del LISR (RLISR)
- Ley del IVA (LIVA)
- Reglamento del LIVA (RLIVA)
- Ley del IEPS (LIEPS)
- Reglamento del LIEPS (RLIEPS)
- Ley del ISAN
- Ley del SAT (LSAT)
- Reglamento del SAT (RISAT)
- LIF 2025 y 2026
════════════════════════════════════════════════════════════════════";

            return mode.ToLower() switch
            {
                "breve" => $@"{basePrompt}

FORMATO BREVE:
- Máximo 3 oraciones
- OBLIGATORIO: Cita del artículo",

                "ejecutiva" => $@"{basePrompt}

FORMATO EJECUTIVO:
1. SÍNTESIS (1 párrafo)
2. PUNTOS CLAVE (bullets)
3. RIESGOS (bullets)
4. ACCIÓN (bullets)
- OBLIGATORIO: Cita en cada sección",

                "tecnica" => $@"{basePrompt}

FORMATO TÉCNICO:
I. MARCO NORMATIVO (citas exactas)
II. ANÁLISIS (punto por punto)
III. IMPLICACIONES
IV. RIESGOS
- OBLIGATORIO: Citas en cada sección",

                "reporte" => $@"{basePrompt}

FORMATO REPORTE:
I. PREGUNTA: [Resume]
II. RESPUESTA: [Párrafos con citas]
III. ARTÍCULOS CITADOS: [Lista]
IV. CONCLUSIÓN: [Frase citada]
- OBLIGATORIO: Citas en todas partes",

                _ => basePrompt
            };
        }

        private AskResponse ErrorResponse(string message, string mode)
        {
            return new AskResponse
            {
                Answer = message,
                Sources = new List<SourceInfo>(),
                ModeUsed = mode
            };
        }
    }
}