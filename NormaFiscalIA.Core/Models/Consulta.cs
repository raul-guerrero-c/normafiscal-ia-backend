using NormaFiscalIA.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NormaFiscalIA.Core.Models
{
    [Table("Consultas")]
    public class Consulta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(5000)]
        public string Contenido { get; set; }

        [Required]
        public ModuloFiscal Modulo { get; set; }

        [Required]
        public TipoRespuesta TipoRespuestaSolicitado { get; set; }

        public TipoRespuesta? TipoRespuestaEntregado { get; set; }

        public MotorIA? MotorUtilizado { get; set; }

        public NivelConfianza? NivelConfianza { get; set; }

        [StringLength(500)]
        public string UsuarioId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        public int? AlucinacionesDetectadas { get; set; }

        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente";

        public virtual Respuesta Respuesta { get; set; }
    }
}
