using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlVueloUsers.Models;

namespace AlVueloUsers.Services
{
    public class CarritoItem
    {
        public Plato Plato { get; set; } = null!;
        public int Cantidad { get; set; } = 1;

        public decimal Subtotal => Plato.Precio * Cantidad;
    }
}

