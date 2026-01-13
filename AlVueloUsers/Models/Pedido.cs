using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlVueloUsers.Models
{
    [Table("Pedido")]
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("cliente_id")]
        public string ClienteId { get; set; }

        [Column("restaurante_id")]
        public string RestauranteId { get; set; }

        [Column("fecha_pedido")]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Column("total")]
        public decimal Total { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Pendiente";

        [Column("tipo_servicio")]
        public string TipoServicio { get; set; }

        [Column("metodo_pago")]
        public string MetodoPago { get; set; }

        [Column("pin")]
        public string Pin { get; set; }

        [Column("mesa_asignada")]
        public string? MesaAsignada { get; set; }
        // Relación con los detalles
        public List<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    }
}