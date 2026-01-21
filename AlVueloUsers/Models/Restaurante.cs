using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlVueloUsers.Models
{
    [Table("Restaurante")]
    public class Restaurante
    {
        public Restaurante()
        {
            Menus = new HashSet<Menu>();
        }

        [Key]
        [Column("id")]
        public string Id { get; set; } = string.Empty;

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("ubicacion")]
        public string Ubicacion { get; set; } = string.Empty;

        [Column("horario")]
        public string Horario { get; set; } = string.Empty;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("campus")]
        public string Campus { get; set; } = string.Empty;

        [Column("logo_url")]
        public string? LogoUrl { get; set; }

        // navegación
        public virtual ICollection<Menu> Menus { get; set; }
    }
}
