using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NormaFiscalIA.Core.Models
{
    [Table("Auditorias")]
    public class Auditoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Accion { get; set; }

        [Required]
        [StringLength(500)]
        public string UsuarioId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Detalles { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Estado { get; set; } = "Exitosa";
    }
}
