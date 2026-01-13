using AlVueloUsers.Data;
using AlVueloUsers.Models;
using Microsoft.EntityFrameworkCore;
#if ANDROID
using Android.Content.Res;
using Android.Graphics;
#endif

namespace AlVueloUsers.Views
{
    public partial class SeleccionMesaPage : ContentPage
    {
        private string _pinPedido;

        public SeleccionMesaPage(string pinPedido)
        {
            InitializeComponent();
            _pinPedido = pinPedido;

            // 1. LLAMADA PARA QUITAR LA BARRA
            QuitarLineaInferiorAndroid();

            // Enfocar el campo automáticamente al abrir
            Loaded += (s, e) => EntryNumeroMesa.Focus();
        }

        // --- MÉTODO PARA ELIMINAR EL SUBRAYADO (ANDROID) ---
        void QuitarLineaInferiorAndroid()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                // Esto hace que el tinte de fondo sea transparente, eliminando la línea
                handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
        }

        private async void OnConfirmarClicked(object sender, EventArgs e)
        {
            // 1. Validaciones básicas de UI
            string inputMesa = EntryNumeroMesa.Text?.Trim();

            if (string.IsNullOrEmpty(inputMesa))
            {
                MostrarError("Por favor escribe un número.");
                return;
            }

            OcultarError();
            BtnConfirmar.IsEnabled = false;
            Spinner.IsVisible = true;
            Spinner.IsRunning = true;

            try
            {
                using (var db = new AlVueloDbContext())
                {
                    // 2. Buscar la mesa en la BD
                    // Buscamos flexiblemente: "5" o "Mesa 5"
                    var mesaDb = await db.Mesas
                        .FirstOrDefaultAsync(m => m.Numero == inputMesa || m.Numero == $"Mesa {inputMesa}");

                    // VALIDACIÓN 1: ¿Existe la mesa?
                    if (mesaDb == null)
                    {
                        MostrarError("Ese número de mesa no existe.");
                        return;
                    }

                    // VALIDACIÓN 2: ¿Está ocupada?
                    if (mesaDb.EstaOcupada)
                    {
                        MostrarError("Esa mesa está ocupada ahora mismo.");
                        return;
                    }

                    // 3. TODO OK: Actualizar BD

                    // A) Marcar mesa ocupada
                    mesaDb.EstaOcupada = true;

                    // B) Asignar al pedido
                    var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Pin == _pinPedido);
                    if (pedido != null)
                    {
                        pedido.MesaAsignada = mesaDb.Numero;
                        pedido.TipoServicio = $"Consumo en Local ({mesaDb.Numero})";
                    }

                    await db.SaveChangesAsync();
                }

                // 4. ÉXITO: Navegar a la pantalla final
                await Navigation.PushModalAsync(new PagoExitosoConsumoPage(_pinPedido));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Ocurrió un problema de conexión.", "OK");
            }
            finally
            {
                // Restaurar UI si falló (si tuvo éxito, ya navegamos fuera)
                if (Navigation.NavigationStack.LastOrDefault() == this)
                {
                    BtnConfirmar.IsEnabled = true;
                    Spinner.IsRunning = false;
                    Spinner.IsVisible = false;
                }
            }
        }

        // --- Métodos visuales para el feedback de error ---
        private void MostrarError(string mensaje)
        {
            LabelError.Text = mensaje;
            LabelError.IsVisible = true;
            LabelError.FadeTo(1, 250);

            // Animación de sacudida (Shake) para indicar error
            EntryNumeroMesa.TranslateTo(-5, 0, 50);
            EntryNumeroMesa.TranslateTo(5, 0, 50);
            EntryNumeroMesa.TranslateTo(-5, 0, 50);
            EntryNumeroMesa.TranslateTo(5, 0, 50);
            EntryNumeroMesa.TranslateTo(0, 0, 50);

            // Habilitar botón de nuevo
            BtnConfirmar.IsEnabled = true;
            Spinner.IsRunning = false;
            Spinner.IsVisible = false;
        }

        private void OcultarError()
        {
            LabelError.FadeTo(0, 100);
            LabelError.IsVisible = false;
        }
    }
}