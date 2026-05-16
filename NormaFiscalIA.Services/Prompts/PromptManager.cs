using NormaFiscalIA.Core.Enums;

namespace NormaFiscalIA.Services.Prompts
{
    public static class PromptManager
    {
        public static string ObtenerSystemPrompt(MotorIA motor, TipoRespuesta tipo, ModuloFiscal modulo)
        {
            return motor == MotorIA.Claude ? ObtenerSystemPromptClaude(tipo) : ObtenerSystemPromptOpenAI(tipo);
        }

        private static string ObtenerSystemPromptClaude(TipoRespuesta tipo)
        {
            return @"IDENTIDAD: NORMAFISCAL IA - Especialista fiscal mexicano
PRINCIPIO: Basarse ÚNICAMENTE en CFF, LISR, LIVA, Reglamentos, Criterios SAT
PROHIBICIONES: NO inventar, NO especular, NO extrapolar
REGLA: Si no está en normas vigentes 2026, DIRLO EXPLÍCITAMENTE";
        }

        private static string ObtenerSystemPromptOpenAI(TipoRespuesta tipo)
        {
            return @"SISTEMA: NORMAFISCAL IA OpenAI
RESTRICCIONES: SOLO CFF, LISR, LIVA, Reglamentos, Criterios SAT vigentes
FORMATO: [Ley, Art. X] - citas exactas
NUNCA inventar disposiciones";
        }

        public static string ObtenerPromptPorTipo(TipoRespuesta tipo, string consulta)
        {
            return tipo switch
            {
                TipoRespuesta.Breve => $@"CONSULTA: {consulta}
FORMATO: Máximo 3 párrafos, citas [LEY, Art. X], respuesta → fundamento → riesgo",
                TipoRespuesta.Ejecutiva => $@"CONSULTA: {consulta}
ESTRUCTURA: 1. SÍNTESIS 2. PUNTOS CLAVE 3. RIESGOS 4. ACCIÓN",
                TipoRespuesta.Tecnica => $@"CONSULTA: {consulta}
ESTRUCTURA: I. MARCO NORMATIVO II. ANÁLISIS III. RIESGOS IV. DOCUMENTACIÓN",
                TipoRespuesta.Informe => $@"CONSULTA: {consulta}
INFORME: I. CONSULTA II. MARCO III. ANÁLISIS IV. CONCLUSIÓN",
                TipoRespuesta.Matriz => $@"CONSULTA: {consulta}
TABLA: OBLIGACIÓN | FUNDAMENTO | SUJETO | PERIODO | SANCIÓN",
                _ => consulta
            };
        }
    }
}
