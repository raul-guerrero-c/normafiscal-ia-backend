using Microsoft.Extensions.Logging;

namespace NormaFiscalIA.Services.Services
{
    /// <summary>
    /// Prompts unificados para Claude y OpenAI
    /// AMBOS reciben EXACTAMENTE las mismas instrucciones
    /// </summary>
    public class UnifiedPromptService
    {
        private readonly ILogger<UnifiedPromptService> _logger;

        public UnifiedPromptService(ILogger<UnifiedPromptService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Obtener prompt base IDÉNTICO para ambos motores
        /// </summary>
        public string GetBasePrompt()
        {
            return @"🔒 RESTRICCIÓN MÁXIMA: SOLO DOCUMENTOS LEGALES
════════════════════════════════════════════════════════════════════

REGLAS OBLIGATORIAS:
1. DEBES responder ÚNICAMENTE con información del Vector Store/Documentos
2. JAMÁS uses conocimiento general o información externa
3. Si NO está en documentos: RESPONDE: 'No encuentro información'
4. NUNCA inventes, adivines o parafrasees sin fuente
5. TODA afirmación DEBE tener cita: [LEY, Art. X]

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
        }

        /// <summary>
        /// Obtener prompt según modo - IDÉNTICO para ambos
        /// </summary>
        public string GetPromptByMode(string mode)
        {
            var basePrompt = GetBasePrompt();

            return mode.ToLower() switch
            {
                "breve" => $@"{basePrompt}

FORMATO: Máximo 3 párrafos
ESTRUCTURA:
- Párrafo 1: Respuesta directa
- Párrafo 2: Detalles importantes
- Párrafo 3: Conclusión
OBLIGATORIO: Incluir cita [LEY, Art. X] en cada párrafo",

                "ejecutiva" => $@"{basePrompt}

FORMATO: Ejecutivo estructurado
ESTRUCTURA OBLIGATORIA:
1. SÍNTESIS (1 párrafo máximo)
2. PUNTOS CLAVE (3-5 bullets)
3. RIESGOS (2-3 bullets)
4. ACCIÓN RECOMENDADA (2-3 bullets)
OBLIGATORIO: Cita [LEY, Art. X] en cada sección",

                "tecnica" => $@"{basePrompt}

FORMATO: Análisis técnico completo
ESTRUCTURA OBLIGATORIA:
I. MARCO NORMATIVO (citas exactas de artículos)
II. ANÁLISIS (explicación detallada)
III. IMPLICACIONES (consecuencias)
IV. RIESGOS (identificar riesgos)
OBLIGATORIO: Citas en cada sección",

                "informe" => $@"{basePrompt}

FORMATO: Reporte completo
ESTRUCTURA OBLIGATORIA:
I. PREGUNTA (resume la consulta)
II. RESPUESTA (párrafos con análisis)
III. ARTÍCULOS CITADOS (lista de artículos)
IV. CONCLUSIÓN (resumen final)
OBLIGATORIO: Citas en todas partes",

                _ => basePrompt
            };
        }
    }
}