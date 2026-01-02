using AlVueloUsers.Data;
using Microsoft.EntityFrameworkCore;

namespace AlVueloUsers.Views
{
    public partial class SeguimientoPage : ContentPage
    {
        private readonly AlVueloDbContext _db = new AlVueloDbContext();
        private int _pedidoId;

        public SeguimientoPage(int pedidoId)
        {
            InitializeComponent();
            _pedidoId = pedidoId;
            CargarInfo();
        }

        private async void CargarInfo()
        {
            var p = await _db.Pedidos.FirstOrDefaultAsync(x => x.Id == _pedidoId);
            if (p != null)
            {
                lblOrderId.Text = $"Orden #{p.Id}";
                lblEstado.Text = p.Estado.ToUpper();

                // Lógica de progreso basada en el script SQL
                barProgreso.Progress = p.Estado switch
                {
                    "Pendiente" => 0.2,
                    "En Preparación" => 0.5,
                    "Listo" => 0.8,
                    "Entregado" => 1.0,
                    _ => 0.0
                };
            }
        }

        private void OnActualizarClicked(object sender, EventArgs e) => CargarInfo();
    }
}