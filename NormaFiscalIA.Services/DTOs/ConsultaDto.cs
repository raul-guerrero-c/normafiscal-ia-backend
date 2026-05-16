using NormaFiscalIA.Core.Enums;

namespace NormaFiscalIA.Services.DTOs
{
    public class ConsultaRequestDto
    {
        public string Consulta { get; set; }
        public ModuloFiscal Modulo { get; set; }
        public TipoRespuesta TipoRespuesta { get; set; } = TipoRespuesta.Breve;
        public bool IncluirRiesgos { get; set; } = true;
        public bool IncluirDocumentacion { get; set; } = true;
        public string FormatoSalida { get; set; } = "json";
        public string UsuarioId { get; set; }
    }

    public class ConsultaResponseDto
    {
        public int Id { get; set; }
        public string Consulta { get; set; }
        public string Respuesta { get; set; }
        public MotorIA MotorUtilizado { get; set; }
        public TipoRespuesta TipoRespuesta { get; set; }
        public MetadataRespuestaDto Metadata { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class MetadataRespuestaDto
    {
        public NivelConfianza NivelConfianza { get; set; }
        public int CitasNormativas { get; set; }
        public int AlucinacionesDetectadas { get; set; }
        public bool EstructuraCompleta { get; set; }
        public TimeSpan TiempoMs { get; set; }
        public List<string> NormasAplicables { get; set; }
    }
}
