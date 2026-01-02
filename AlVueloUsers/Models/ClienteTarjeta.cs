using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlVueloUsers.Models
{
    [Table("Cliente_Tarjeta")]
    public class ClienteTarjeta
    {
        [Column("cliente_id")]
        public string ClienteId { get; set; }

        [Column("num_tarjeta")]
        public string NumTarjeta { get; set; }
    }
}
