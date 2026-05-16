using NormaFiscalIA.Core.Enums;
using NormaFiscalIA.Services.Interfaces;
using NormaFiscalIA.Services.Prompts;
using Microsoft.Extensions.Logging;

namespace NormaFiscalIA.Services.Services
{
    public class RouterIA : IRouterIA
    {
        private readonly IClaudeService _claudeService;
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<RouterIA> _logger;

        public RouterIA(IClaudeService claudeService, IOpenAIService openAIService, ILogger<RouterIA> logger)
        {
            _claudeService = claudeService;
            _openAIService = openAIService;
            _logger = logger;
        }

        public async Task<MotorIA> DeterminarMotorAsync(string consulta, TipoRespuesta tipoRespuesta)
        {
            if (tipoRespuesta == TipoRespuesta.Breve || tipoRespuesta == TipoRespuesta.Matriz)
                return MotorIA.Claude;
            return tipoRespuesta == TipoRespuesta.Tecnica || tipoRespuesta == TipoRespuesta.Informe
                ? MotorIA.OpenAI
                : MotorIA.Claude;
        }

        public async Task<string> ProcesarConsultaAsync(string consulta, ModuloFiscal modulo, TipoRespuesta tipoRespuesta, MotorIA motor)
        {
            try
            {
                _logger.LogInformation($"Procesando con {motor}");
                var systemPrompt = PromptManager.ObtenerSystemPrompt(motor, tipoRespuesta, modulo);
                var userPrompt = PromptManager.ObtenerPromptPorTipo(tipoRespuesta, consulta);

                return motor == MotorIA.Claude
                    ? await _claudeService.ConsultarAsync(userPrompt, systemPrompt, 1500)
                    : await _openAIService.ConsultarAsync(userPrompt, systemPrompt, 2000);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                throw;
            }
        }
    }
}
