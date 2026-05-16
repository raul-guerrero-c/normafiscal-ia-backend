using NormaFiscalIA.Core.Enums;

namespace NormaFiscalIA.Services.Interfaces
{
    public interface IRouterIA
    {
        Task<MotorIA> DeterminarMotorAsync(string consulta, TipoRespuesta tipoRespuesta);
        Task<string> ProcesarConsultaAsync(string consulta, ModuloFiscal modulo, TipoRespuesta tipoRespuesta, MotorIA motor);
    }
}
