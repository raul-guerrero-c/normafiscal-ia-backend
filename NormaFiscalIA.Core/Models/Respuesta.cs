using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NormaFiscalIA.Core.Models
{
    [Table("Respuestas")]
    public class Respuesta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ConsultaId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Contenido { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string ContenidoValidacion { get; set; }

        public int CitasNormativas { get; set; }

        public int AlucinacionesDetectadas { get; set; }

        public bool EstructuraCompleta { get; set; }

        public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;

        public TimeSpan TiempoProcesamiento { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string MetadataIA { get; set; }

        public virtual Consulta Consulta { get; set; }
    }
}
