namespace NormaFiscalIA.Services.DTOs
{
    public class ValidacionRespuestaDto
    {
        public DateTime Timestamp { get; set; }
        public TrazabilidadDto Trazabilidad { get; set; }
        public bool Alucinaciones { get; set; }
        public EstructuraDto Estructura { get; set; }
        public string NivelConfianza { get; set; }
    }

    public class TrazabilidadDto
    {
        public int TotalCitas { get; set; }
        public string NivelCobertura { get; set; }
    }

    public class EstructuraDto
    {
        public int ElementosEsperados { get; set; }
        public int ElementosEncontrados { get; set; }
        public string Completitud { get; set; }
    }
}
