using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlVueloUsers.Models
{
    [Table("Plato")]
    public class Plato
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("ingredientes")]
        public string Ingredientes { get; set; } = string.Empty;

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("disponibilidad")]
        public bool Disponibilidad { get; set; } = true;

        [Column("imagen_url")]
        public string? ImagenUrl { get; set; }

        [Column("menu_id")]
        public int MenuId { get; set; }

        // navegación
        public Menu? Menu { get; set; }
    }
}
