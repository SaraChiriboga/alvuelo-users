using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlVueloUsers.Models
{
    [Table("Tarjeta")]
    public class Tarjeta
    {
        [Key]
        [Column("num_tarjeta")] // Nombre real en SQL
        public string NumTarjeta { get; set; }

        [Column("nombre_titular")]
        public string NombreTitular { get; set; }

        [Column("fecha_expiracion")]
        public string FechaExpiracion { get; set; }

        [Column("cvv")]
        public string Cvv { get; set; }
    }
}