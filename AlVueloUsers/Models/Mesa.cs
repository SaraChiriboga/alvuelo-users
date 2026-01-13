using System.ComponentModel.DataAnnotations;

namespace AlVueloUsers.Models
{
    public class Mesa
    {
        [Key]
        public int Id { get; set; }
        public string Numero { get; set; } // Ej: "Mesa 1", "Mesa 2"
        public bool EstaOcupada { get; set; }
    }
}