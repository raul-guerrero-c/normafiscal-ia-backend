using NormaFiscalIA.Services.DTOs;
using NormaFiscalIA.Services.Interfaces;
using System.Text.RegularExpressions;

namespace NormaFiscalIA.Services.Services
{
    public class ValidadorService : IValidadorService
    {
        private static readonly string[] Prohibidas = new[] { "generalmente", "típicamente", "probablemente" };
        private static readonly string[] Patrones = new[] { @"\[CFF, Art\. \d+", @"\[LISR, Art\. \d+" };

        public ValidacionRespuestaDto ValidarRespuesta(string respuesta, string tipoRespuesta)
        {
            return new ValidacionRespuestaDto
            {
                Timestamp = DateTime.UtcNow,
                Trazabilidad = new TrazabilidadDto { TotalCitas = ContarCitas(respuesta), NivelCobertura = ContarCitas(respuesta) > 0 ? "Alto" : "Bajo" },
                Alucinaciones = DetectarAlucinaciones(respuesta),
                Estructura = new EstructuraDto { ElementosEsperados = 3, ElementosEncontrados = 3, Completitud = "3/3" },
                NivelConfianza = ContarCitas(respuesta) > 3 && !DetectarAlucinaciones(respuesta) ? "Alta" : "Media"
            };
        }

        public bool TieneCitasNormativas(string respuesta) => Patrones.Any(p => Regex.IsMatch(respuesta, p));
        public int ContarCitas(string respuesta) { int c = 0; foreach (var p in Patrones) c += Regex.Matches(respuesta, p).Count; return c; }
        public bool DetectarAlucinaciones(string respuesta) => Prohibidas.Any(p => respuesta.ToLower().Contains(p));
    }
}
