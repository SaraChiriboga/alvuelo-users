using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alvueloapp.Models
{
    [Table("Cliente")]
    public class Cliente
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }

        [Required]
        [Column("idBanner")]
        public string IdBanner { get; set; } = "";

        [Column("nombre")]
        public string Nombre { get; set; } = "";

        [Required]
        [Column("correo")]
        public string Correo { get; set; } = "";

        [Required]
        [Column("password")]
        public string Password { get; set; } = "";

        [Required]
        [Column("telefono")]
        public string Telefono { get; set; } = "";

        [Column("imagen_url")]
        public string? Imagen_Url { get; set; } = "";
    }
}
