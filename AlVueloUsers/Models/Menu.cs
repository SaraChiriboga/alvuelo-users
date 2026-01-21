using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlVueloUsers.Models
{
    [Table("Menu")]
    public class Menu
    {
        public Menu()
        {
            Platos = new HashSet<Plato>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("categoria")]
        public string Categoria { get; set; } = string.Empty;

        [Column("restaurante_id")]
        public string RestauranteId { get; set; } = string.Empty;

        // navegación
        public Restaurante? Restaurante { get; set; }

        public virtual ICollection<Plato> Platos { get; set; }
    }
}
