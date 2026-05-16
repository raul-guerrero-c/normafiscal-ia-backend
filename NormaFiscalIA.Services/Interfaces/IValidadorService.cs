using NormaFiscalIA.Services.DTOs;

namespace NormaFiscalIA.Services.Interfaces
{
    public interface IValidadorService
    {
        ValidacionRespuestaDto ValidarRespuesta(string respuesta, string tipoRespuesta);
        bool TieneCitasNormativas(string respuesta);
        int ContarCitas(string respuesta);
        bool DetectarAlucinaciones(string respuesta);
    }
}
