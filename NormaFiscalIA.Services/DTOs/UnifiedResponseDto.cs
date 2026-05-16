namespace NormaFiscalIA.Services.DTOs
{
    /// <summary>
    /// MISMO JSON para Claude y OpenAI
    /// Ambos endpoints devuelven IDÉNTICA estructura
    /// </summary>
    public class UnifiedResponseDto
    {
        public bool Exito { get; set; }
        public string Motor { get; set; }
        public ConsultaDatosDto Datos { get; set; }
        public string Mensaje { get; set; }
    }

    public class ConsultaDatosDto
    {
        public int Id { get; set; }
        public string Consulta { get; set; }
        public string Respuesta { get; set; }
        public string MotorUtilizado { get; set; }
        public int TipoRespuesta { get; set; }
        public MetadataDto Metadata { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class MetadataDto
    {
        public int CitasNormativas { get; set; }
        public int AlucinacionesDetectadas { get; set; }
        public bool EstructuraCompleta { get; set; }
        public string TiempoMs { get; set; }
        public int NivelConfianza { get; set; }
        public List<string> NormasAplicables { get; set; }
    }
}