using AlVueloUsers.Models;
using System.Collections.ObjectModel;

namespace AlVueloUsers.Services
{
    public class CarritoService
    {
        public static CarritoService Instancia { get; } = new();

        public ObservableCollection<CarritoItem> Items { get; } = new();

        // Subtotal real (suma de subtotales de cada item)
        public decimal Subtotal => Items.Sum(i => i.Subtotal);

        // Servicio (ejemplo fijo, puedes cambiar la regla)
        public decimal Servicio => Items.Count > 0 ? 0.50m : 0m;

        // Total final
        public decimal Total => Subtotal + Servicio;

        public void Add(Plato plato)
        {
            if (plato == null) return;

            var existente = Items.FirstOrDefault(i => i.Plato?.Id == plato.Id);

            if (existente != null)
                existente.Cantidad++;
            else
                Items.Add(new CarritoItem { Plato = plato, Cantidad = 1 });
        }

        public void Incrementar(CarritoItem item)
        {
            if (item == null) return;
            item.Cantidad++;
        }

        public void Decrementar(CarritoItem item)
        {
            if (item == null) return;

            item.Cantidad--;
            if (item.Cantidad <= 0)
                Items.Remove(item);
        }

        public void Clear() => Items.Clear();
    }
}
