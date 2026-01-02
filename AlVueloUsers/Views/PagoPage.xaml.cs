using AlVueloUsers.Data;
using AlVueloUsers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;

namespace AlVueloUsers.Views
{
    public partial class PagoPage : ContentPage
    {
        // Variables para rastrear selecciones actuales
        private Border _servicioSeleccionadoActual;
        private VisualElement _metodoPagoSeleccionadoActual;

        public PagoPage()
        {
            InitializeComponent();

            // 1. Ocultar la navegación nativa
            NavigationPage.SetHasNavigationBar(this, false);

            // 2. Configuración por defecto del Campus y ETA
            PickerCampus.SelectedIndex = 0; // UdlaPark por defecto
            UpdateEstimatedTime();

            // 3. Suscribirse al evento de cambio de campus
            PickerCampus.SelectedIndexChanged += OnCampusChanged;

            // 4. Cargar datos de SQL (Servidor SARILOLA)
            CargarTarjetaDesdeBD();

            // 5. Configurar visuales iniciales de servicios
            SetupInitialServicios();
        }

        private void SetupInitialServicios()
        {
            var campus = this.FindByName<Border>("BorderCampus");
            var retiro = this.FindByName<Border>("BorderRetiro");
            var consumo = this.FindByName<Border>("BorderConsumo");

            ResetServicioVisuals(campus);
            ResetServicioVisuals(retiro);
            ResetServicioVisuals(consumo);

            // Seleccionar Campus por defecto visualmente
            if (campus != null)
            {
                ApplyServicioSelectedVisuals(campus);
                _servicioSeleccionadoActual = campus;
            }
        }

        private void OnCampusChanged(object sender, EventArgs e)
        {
            UpdateEstimatedTime();
        }

        private void UpdateEstimatedTime()
        {
            if (PickerCampus.SelectedItem == null) return;

            string selectedCampus = PickerCampus.SelectedItem.ToString();

            // Actualización del LabelTiempoEstimado según el campus seleccionado
            switch (selectedCampus)
            {
                case "UdlaPark":
                    LabelTiempoEstimado.Text = "15 - 20 mins";
                    break;
                case "Granados":
                    LabelTiempoEstimado.Text = "25 - 35 mins";
                    break;
                case "Colón":
                    LabelTiempoEstimado.Text = "40 - 50 mins";
                    break;
                default:
                    LabelTiempoEstimado.Text = "Calculando...";
                    break;
            }
        }

        private async void CargarTarjetaDesdeBD()
        {
            try
            {
                using (var db = new AlVueloDbContext())
                {
                    // Consulta con Join entre Cliente_Tarjeta y Tarjeta para Ana Martínez (C001)
                    var tarjetaInfo = await (from ct in db.Set<ClienteTarjeta>()
                                             join t in db.Tarjetas on ct.NumTarjeta equals t.NumTarjeta
                                             where ct.ClienteId == "C001"
                                             select t).FirstOrDefaultAsync();

                    if (tarjetaInfo != null && !string.IsNullOrEmpty(tarjetaInfo.NumTarjeta))
                    {
                        string ultimosCuatro = tarjetaInfo.NumTarjeta.Substring(tarjetaInfo.NumTarjeta.Length - 4);
                        LabelNombreTarjeta.Text = $"Mi Tarjeta •••• {ultimosCuatro}";

                        if (tarjetaInfo.NumTarjeta.StartsWith("4"))
                        {
                            ActualizarIconoVisa();
                        }
                    }
                    else
                    {
                        LabelNombreTarjeta.Text = "No tienes tarjetas vinculadas";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SQL: {ex.Message}");
                LabelNombreTarjeta.Text = "Error al conectar con el servidor";
            }
        }

        private void ActualizarIconoVisa()
        {
            if (IconoTarjeta.Source is FontImageSource source)
            {
                source.Glyph = "\uf1f0";
                source.Color = Color.FromArgb("#1A1F71");
            }
        }

        // --- MÉTODOS VISUALES DE SERVICIO ---

        private void ResetServicioVisuals(Border b)
        {
            if (b == null) return;
            b.Stroke = new SolidColorBrush(Color.FromArgb("#E0E0E0"));
            b.StrokeThickness = 1;
            b.BackgroundColor = Colors.White;
        }

        private Color GetAppColor(string key, Color fallback)
        {
            var appResources = Application.Current?.Resources;
            if (appResources != null && appResources.ContainsKey(key) && appResources[key] is Color c)
                return c;
            return fallback;
        }

        private void ApplyServicioSelectedVisuals(Border b)
        {
            if (b == null) return;
            var red = GetAppColor("AlVueloRed", Colors.Red);
            b.Stroke = new SolidColorBrush(red);
            b.StrokeThickness = 1;
            b.BackgroundColor = Colors.White;
        }

        private async void OnServicioSelected(object sender, EventArgs e)
        {
            Border border = sender as Border;
            if (border == null) return;

            // Feedback de oscurecimiento (Hover)
            var originalColor = border.BackgroundColor;
            border.BackgroundColor = Color.FromArgb("#F0F0F0");
            await Task.Delay(100);
            border.BackgroundColor = originalColor;

            if (_servicioSeleccionadoActual != null && _servicioSeleccionadoActual != border)
            {
                ResetServicioVisuals(_servicioSeleccionadoActual);
            }

            ApplyServicioSelectedVisuals(border);
            _servicioSeleccionadoActual = border;

            // Animación de clic
            await border.ScaleTo(0.98, 50);
            await border.ScaleTo(1.0, 50);
        }

        // --- MÉTODOS VISUALES DE PAGO ---

        private async void OnMetodoPagoSelected(object sender, EventArgs e)
        {
            var layout = sender as VisualElement;
            if (layout == null) return;

            // Feedback de oscurecimiento (Hover)
            layout.BackgroundColor = Color.FromArgb("#F7F7F7");

            if (_metodoPagoSeleccionadoActual != null && _metodoPagoSeleccionadoActual != layout)
            {
                ResetMetodoPagoVisuals(_metodoPagoSeleccionadoActual);
            }

            ApplyMetodoPagoSelectedVisuals(layout);
            _metodoPagoSeleccionadoActual = layout;

            await layout.ScaleTo(0.98, 50);
            await layout.ScaleTo(1.0, 50);
        }

        private void ResetMetodoPagoVisuals(VisualElement element)
        {
            var textSecondary = GetAppColor("TextSecondary", Colors.Gray);
            SetMetodoVisuals(element, textSecondary, Colors.Transparent);
        }

        private void ApplyMetodoPagoSelectedVisuals(VisualElement element)
        {
            var red = GetAppColor("AlVueloRed", Colors.Red);
            SetMetodoVisuals(element, red, red);
        }

        private void SetMetodoVisuals(VisualElement element, Color textColor, Color borderIconColor)
        {
            if (element == GridMetodo_Tarjeta) { LabelNombreTarjeta.TextColor = textColor; BorderIcon_Tarjeta.Stroke = borderIconColor; }
            else if (element == GridMetodo_Deuna) { LabelDeuna.TextColor = textColor; BorderIcon_Deuna.Stroke = borderIconColor; }
            else if (element == GridMetodo_Transfer) { LabelTransfer.TextColor = textColor; BorderIcon_Transfer.Stroke = borderIconColor; }
            else if (element == GridMetodo_Efectivo) { LabelEfectivo.TextColor = textColor; BorderIcon_Efectivo.Stroke = borderIconColor; }
            else if (element == GridMetodo_Agregar) { LabelAgregar.TextColor = textColor; BorderIcon_Agregar.Stroke = borderIconColor; }
        }
    }
}