namespace NormaFiscalIA.Core.Enums
{
    public enum ModuloFiscal
    {
        CumplimientoTributario = 1,
        AuditoriaFiscal = 2,
        InvestigacionNormativa = 3,
        OperacionesDocumentacion = 4
    }

    public enum TipoRespuesta
    {
        Breve = 1,
        Ejecutiva = 2,
        Tecnica = 3,
        Informe = 4,
        Matriz = 5
    }

    public enum MotorIA
    {
        Claude = 1,
        OpenAI = 2
    }

    public enum NivelConfianza
    {
        Baja = 1,
        Media = 2,
        Alta = 3
    }
}
