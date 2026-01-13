using AlVueloUsers.Data;
using AlVueloUsers.Helpers; // Asegúrate de tener tu helper de GooglePolyline
using AlVueloUsers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Text.Json;

namespace AlVueloUsers.Views
{
    public partial class SeguimientoEntregaPage : ContentPage
    {
        // IMPORTANTE: Pon aquí tu API Key real de Google Maps
        private const string GOOGLE_MAPS_API_KEY = "AIzaSyBQe2aEkpl9HGd8nvP0zlcywPzAid_qm9w";

        private System.Timers.Timer _updateTimer;
        private string _campusDestino = "UdlaPark"; // Valor por defecto

        // Variables para el gesto de arrastre
        private double _startY;
        private bool _isCollapsed = false;

        // Constructor modificado para recibir el PIN
        public SeguimientoEntregaPage(string pinRecibido)
        {
            InitializeComponent();

            // 1. Mostrar el PIN inmediatamente (lo que recibimos de la pantalla anterior)
            MostrarPinEnPantalla(pinRecibido);

            // 2. Mostrar loading mientras carga el mapa y ruta
            LoadingOverlay.IsVisible = true;

            // 3. Iniciar carga de datos de BD y Mapa
            Task.Run(async () =>
            {
                // Primero cargamos el pedido para saber el Campus real
                await CargarDatosPedido();

                // Luego cargamos la ruta basada en ese campus
                await CargarRutaEnMapa();

                // Quitamos el loading y animamos
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadingOverlay.IsVisible = false;
                    AnimarEntrada();
                });
            });

            // 4. Actualizar ubicación/estado cada 30 segundos
            IniciarActualizacionPeriodica();
        }

        private void MostrarPinEnPantalla(string pin)
        {
            if (string.IsNullOrEmpty(pin)) return;

            // Rellenar los 4 dígitos
            PinDigit1.Text = pin.Length > 0 ? pin[0].ToString() : "-";
            PinDigit2.Text = pin.Length > 1 ? pin[1].ToString() : "-";
            PinDigit3.Text = pin.Length > 2 ? pin[2].ToString() : "-";
            PinDigit4.Text = pin.Length > 3 ? pin[3].ToString() : "-";
        }

        private async Task CargarDatosPedido()
        {
            try
            {
                using (var db = new AlVueloDbContext())
                {
                    // Buscamos el último pedido de este cliente
                    var ultimoPedido = await db.Pedidos
                        .OrderByDescending(p => p.Id)
                        .FirstOrDefaultAsync(p => p.ClienteId == "C001");

                    if (ultimoPedido != null)
                    {
                        // Detectar Campus desde el string "TipoServicio" 
                        // Ej: "Entrega campus (Granados)"
                        if (ultimoPedido.TipoServicio != null)
                        {
                            if (ultimoPedido.TipoServicio.Contains("Granados")) _campusDestino = "Granados";
                            else if (ultimoPedido.TipoServicio.Contains("Colón")) _campusDestino = "Colón";
                            else _campusDestino = "UdlaPark";
                        }

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Actualizar estado visual (preparando, en camino...)
                            ActualizarEstadoPedido(ultimoPedido.Estado ?? "En Preparación");

                            // (Opcional) Confirmar PIN desde BD por si acaso
                            if (!string.IsNullOrEmpty(ultimoPedido.Pin))
                            {
                                MostrarPinEnPantalla(ultimoPedido.Pin);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar pedido: {ex.Message}");
            }
        }

        private async Task CargarRutaEnMapa()
        {
            // Coordenadas
            var locationRestaurante = new Location(-0.1715, -78.4755); // UPO1 (Ejemplo)
            Location locationDestino;

            // Definir coordenadas según el campus detectado
            switch (_campusDestino)
            {
                case "Granados":
                    locationDestino = new Location(-0.1715, -78.4755);
                    break;
                case "Colón":
                    locationDestino = new Location(-0.2014, -78.4912);
                    break;
                case "UdlaPark":
                default:
                    locationDestino = new Location(-0.1663, -78.4633);
                    break;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (MapaSeguimiento == null) return;

                MapaSeguimiento.Pins.Clear();
                MapaSeguimiento.MapElements.Clear();

                // Pin Restaurante
                MapaSeguimiento.Pins.Add(new Pin
                {
                    Label = "AlVuelo Restaurant",
                    Address = "Preparando pedido",
                    Type = PinType.Place,
                    Location = locationRestaurante
                });

                // Pin Destino
                MapaSeguimiento.Pins.Add(new Pin
                {
                    Label = "Tu ubicación",
                    Address = $"Campus {_campusDestino}",
                    Type = PinType.SavedPin,
                    Location = locationDestino
                });
            });

            // Obtener ruta de Google API
            try
            {
                string json = await ObtenerRutaGoogle(
                    locationRestaurante.Latitude, locationRestaurante.Longitude,
                    locationDestino.Latitude, locationDestino.Longitude
                );

                if (!string.IsNullOrEmpty(json))
                {
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "OK")
                        {
                            var route = root.GetProperty("routes")[0];
                            var leg = route.GetProperty("legs")[0];

                            string tiempoTexto = leg.GetProperty("duration").GetProperty("text").GetString();
                            string distanciaTexto = leg.GetProperty("distance").GetProperty("text").GetString();
                            string encodedPolyline = route.GetProperty("overview_polyline").GetProperty("points").GetString();

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                LabelDistancia.Text = distanciaTexto;
                                CalcularTiempoEstimado(tiempoTexto);
                            });

                            // Dibujar Polilínea
                            var puntosRuta = GooglePolylineHelper.Decode(encodedPolyline);

                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                var lineaRuta = new Polyline
                                {
                                    StrokeColor = Color.FromArgb("#FF5757"),
                                    StrokeWidth = 6
                                };

                                foreach (var punto in puntosRuta)
                                    lineaRuta.Geopath.Add(punto);

                                MapaSeguimiento.MapElements.Add(lineaRuta);

                                // Zoom para ver toda la ruta
                                var centerLat = (locationRestaurante.Latitude + locationDestino.Latitude) / 2;
                                var centerLon = (locationRestaurante.Longitude + locationDestino.Longitude) / 2;

                                // Ajustar el radio del zoom un poco más amplio
                                MapaSeguimiento.MoveToRegion(MapSpan.FromCenterAndRadius(
                                    new Location(centerLat, centerLon),
                                    Distance.FromKilometers(3)));
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ruta Google: {ex.Message}");
                // Fallback visual si falla Google API (Línea recta)
                DibujarLineaRectaFallback(locationRestaurante, locationDestino);
            }
        }

        private void DibujarLineaRectaFallback(Location inicio, Location fin)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var linea = new Polyline
                {
                    StrokeColor = Colors.Gray,
                    StrokeWidth = 4
                };
                linea.Geopath.Add(inicio);
                linea.Geopath.Add(fin);
                MapaSeguimiento.MapElements.Add(linea);

                MapaSeguimiento.MoveToRegion(MapSpan.FromCenterAndRadius(inicio, Distance.FromKilometers(3)));
            });
        }

        private async Task<string> ObtenerRutaGoogle(double latOrigen, double lonOrigen, double latDestino, double lonDestino)
        {
            // Recuerda habilitar "Directions API" en tu consola de Google Cloud
            string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={latOrigen},{lonOrigen}&destination={latDestino},{lonDestino}&mode=driving&key={GOOGLE_MAPS_API_KEY}";

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch { }
            }
            return null;
        }

        private void CalcularTiempoEstimado(string duracion)
        {
            var ahora = DateTime.Now;
            int minutos = 20;

            try
            {
                if (!string.IsNullOrEmpty(duracion))
                {
                    // Extraer número simple del string "15 mins"
                    var partes = duracion.Split(' ');
                    if (int.TryParse(partes[0], out int n))
                        minutos = n;
                }
            }
            catch { }

            var horaMin = ahora.AddMinutes(minutos);
            var horaMax = ahora.AddMinutes(minutos + 10);
            LabelTiempoEstimado.Text = $"{horaMin:HH:mm} - {horaMax:HH:mm}";
        }

        private void ActualizarEstadoPedido(string estado)
        {
            switch (estado)
            {
                case "En Preparación":
                case "Pendiente":
                    LabelEstado.Text = "Preparando tu pedido";
                    LabelDescripcionEstado.Text = "El restaurante está preparando tu orden";
                    break;
                case "Listo":
                    LabelEstado.Text = "Listo para entregar";
                    LabelDescripcionEstado.Text = "Esperando al repartidor";
                    break;
                case "En Camino":
                    LabelEstado.Text = "Pedido en camino";
                    LabelDescripcionEstado.Text = "El repartidor se acerca a tu ubicación";
                    break;
                case "Entregado":
                    LabelEstado.Text = "¡Pedido entregado!";
                    LabelDescripcionEstado.Text = "Disfruta tu comida";
                    break;
                default:
                    LabelEstado.Text = estado;
                    break;
            }
        }

        private void IniciarActualizacionPeriodica()
        {
            _updateTimer = new System.Timers.Timer(30000);
            _updateTimer.Elapsed += async (s, e) =>
            {
                await CargarDatosPedido();
                // Aquí en el futuro podrías actualizar la posición del motorizado
            };
            _updateTimer.Start();
        }

        private async void AnimarEntrada()
        {
            BottomSheet.TranslationY = 300;
            await BottomSheet.TranslateTo(0, 0, 500, Easing.CubicOut);
        }

        // --- GESTOS DE ARRASTRE (BottomSheet) ---
        private void OnBottomSheetPan(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startY = BottomSheet.TranslationY;
                    break;

                case GestureStatus.Running:
                    var newY = _startY + e.TotalY;
                    // Límites: 0 (arriba) a 380 (abajo)
                    if (newY >= 0 && newY <= 380)
                    {
                        BottomSheet.TranslationY = newY;
                    }
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    // Snap effect
                    if (BottomSheet.TranslationY > 100) CollapseBottomSheet();
                    else ExpandBottomSheet();
                    break;
            }
        }

        private async void CollapseBottomSheet()
        {
            await BottomSheet.TranslateTo(0, 350, 250, Easing.SpringOut);
            _isCollapsed = true;
        }

        private async void ExpandBottomSheet()
        {
            await BottomSheet.TranslateTo(0, 0, 250, Easing.SpringOut);
            _isCollapsed = false;
        }

        // --- BOTONES ---
        private async void OnLlamarClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement v) await v.ScaleEffect(); // Extension helper o animación manual

            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open("0991234567");
            }
            catch
            {
                await DisplayAlert("Error", "No se puede llamar", "OK");
            }
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement v) await v.ScaleTo(0.95, 100).ContinueWith(t => v.ScaleTo(1, 100));

            _updateTimer?.Stop();
            // Regresar al Home y borrar historial de navegación
            Application.Current.MainPage = new NavigationPage(new PagoPage()); // O tu HomePage real
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _updateTimer?.Stop();
        }
    }

    // Pequeño helper para la animación de click si no tienes uno global
    public static class ViewExtensions
    {
        public static async Task ScaleEffect(this VisualElement view)
        {
            await view.ScaleTo(0.95, 80);
            await view.ScaleTo(1.0, 80);
        }
    }
}