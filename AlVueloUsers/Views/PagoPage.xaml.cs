using AlVueloUsers.Data;
using AlVueloUsers.Models;
using AlVueloUsers.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;

namespace AlVueloUsers.Views
{
    public partial class PagoPage : ContentPage
    {
        // Variables para rastrear selecciones actuales
        private Border _servicioSeleccionadoActual;
        private VisualElement _metodoPagoSeleccionadoActual;

        // VALORES ECONÓMICOS
        private decimal _subtotalProductos = 0.96m;
        private decimal _tarifaServicio = 0.96m;
        private decimal _costoEnvio = 0.00m; // Cambia dinámicamente según el servicio

        public PagoPage()
        {
            InitializeComponent();

            // 1. Ocultar la navegación nativa
            NavigationPage.SetHasNavigationBar(this, false);

            // Suscripciones a eventos de RadioButtons
            if (RadioTarjeta != null) RadioTarjeta.CheckedChanged += OnRadioMetodoCheckedChanged;
            if (RadioDeuna != null) RadioDeuna.CheckedChanged += OnRadioMetodoCheckedChanged;
            if (RadioTransfer != null) RadioTransfer.CheckedChanged += OnRadioMetodoCheckedChanged;
            if (RadioEfectivo != null) RadioEfectivo.CheckedChanged += OnRadioMetodoCheckedChanged;

            // 2. Configuración por defecto
            PickerCampus.SelectedIndex = 0; // UdlaPark por defecto
            UpdateEstimatedTime();

            // 3. Suscribirse al evento de cambio de campus
            PickerCampus.SelectedIndexChanged += OnCampusChanged;

            // 4. Cargar datos
            CargarTarjetaDesdeBD();
            SetupInitialServicios();
            UpdatePaymentMethodsAvailability();

            // 5. Inicializar el total visualmente (Calcula Envío + Total)
            UpdateEnvioAmountForService();

            // Forzar la selección visual inicial de la Tarjeta si existe
            if (GridMetodo_Tarjeta != null)
            {
                OnMetodoPagoSelected(GridMetodo_Tarjeta, EventArgs.Empty);
            }
        }

        // --- MÉTODOS DE CÁLCULO DE TOTALES ---

        private void ActualizarTotal()
        {
            // Cálculo del Total Real
            decimal totalCalculado = _subtotalProductos + _tarifaServicio + _costoEnvio;

            // Actualizar Label Total en la UI
            if (LabelTotal != null)
            {
                LabelTotal.Text = $"${totalCalculado:F2}";
            }

            // Actualizar Botón Pagar para reflejar el monto
            if (BtnPagar != null && BtnPagar.IsEnabled)
            {
                BtnPagar.Text = $"Confirmar y Pagar (${totalCalculado:F2})";
            }
        }

        private void UpdateEnvioAmountForService()
        {
            if (_servicioSeleccionadoActual == BorderCampus)
            {
                _costoEnvio = 0.50m;
            }
            else
            {
                _costoEnvio = 0.00m; // Retiro o Consumo es gratis
            }

            // Actualizar etiqueta visual
            if (LabelEnvioAmount != null)
            {
                LabelEnvioAmount.Text = $"${_costoEnvio:F2}";
            }

            // Recalcular el total general
            ActualizarTotal();
        }

        // --- LÓGICA DE INTERACCIÓN Y NAVEGACIÓN ---

        private async void OnMetodoPagoSelected(object sender, EventArgs e)
        {
            var layout = sender as VisualElement;
            if (layout == null || layout.Opacity < 1.0) return;

            var tapped = e as TappedEventArgs;
            string comando = tapped?.Parameter?.ToString();

            // CASO ESPECIAL: BOTÓN "AGREGAR NUEVA TARJETA"
            if (comando == "Agregar")
            {
                // Calculamos el total actual para pasarlo a la siguiente página
                decimal totalActual = _subtotalProductos + _tarifaServicio + _costoEnvio;

                // Navegamos pasando el total y el ID del cliente
                await Navigation.PushAsync(new AgregarTarjetaPage(totalActual, "C001"));
                return;
            }

            // Gestión de cambio de selección visual
            if (_metodoPagoSeleccionadoActual != null && _metodoPagoSeleccionadoActual != layout)
            {
                ResetMetodoPagoVisuals(_metodoPagoSeleccionadoActual);
                UncheckRadioOfElement(_metodoPagoSeleccionadoActual);
            }

            ApplyMetodoPagoSelectedVisuals(layout);
            CheckRadioOfElement(layout);
            _metodoPagoSeleccionadoActual = layout;

            // Animación de feedback
            await layout.ScaleTo(0.98, 50);
            await layout.ScaleTo(1.0, 50);
        }

        private async void OnPagarClicked(object sender, EventArgs e)
        {
            if (_metodoPagoSeleccionadoActual == null)
            {
                await DisplayAlert("Error", "Selecciona un método de pago.", "OK");
                return;
            }

            // Calcular Total Final al momento del clic
            decimal totalFinal = _subtotalProductos + _tarifaServicio + _costoEnvio;

            // Guardar estado visual del botón
            string textoOriginal = BtnPagar.Text;

            // Poner UI en estado de carga
            BtnPagar.Text = "";
            BtnPagar.IsEnabled = false;
            SpinnerCarga.IsVisible = true;
            SpinnerCarga.IsRunning = true;

            try
            {
                bool pagoAprobado = false;
                string pinParaEnviar = "";

                if (_metodoPagoSeleccionadoActual == GridMetodo_Tarjeta)
                {
                    var tarjetaInfo = await ObtenerTarjetaParaPago("C001");

                    if (tarjetaInfo != null)
                    {
                        var servicio = new PayPalService();

                        // Llamada al servicio con el TOTAL DINÁMICO
                        var resultado = await servicio.ProcesarPago(
                            totalFinal,
                            tarjetaInfo.NumTarjeta,
                            tarjetaInfo.FechaExpiracion,
                            tarjetaInfo.Cvv,
                            tarjetaInfo.NombreTitular
                        );

                        if (resultado.Exito)
                        {
                            pagoAprobado = true;
                            pinParaEnviar = await RegistrarPedidoEnBD("Pagado (PayPal)", totalFinal);
                        }
                        else
                        {
                            await DisplayAlert("Error", resultado.Mensaje, "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se encontró tarjeta guardada.", "OK");
                    }
                }
                else
                {
                    // Lógica para Efectivo o Transferencia
                    pagoAprobado = true;
                    pinParaEnviar = await RegistrarPedidoEnBD("Pendiente de pago", totalFinal);
                }

                // --- LÓGICA DE NAVEGACIÓN CORREGIDA ---
                if (pagoAprobado)
                {
                    // 1. SI ES CONSUMO EN LOCAL -> IR A SELECCIONAR MESA (Pasando el PIN)
                    if (_servicioSeleccionadoActual == BorderConsumo)
                    {
                        // Navegamos a la selección de mesa en lugar de la pantalla de éxito
                        await Navigation.PushAsync(new SeleccionMesaPage(pinParaEnviar));
                    }
                    // 2. SI ES ENTREGA O RETIRO -> IR A PANTALLA DE ÉXITO DIRECTAMENTE
                    else
                    {
                        Page paginaDestino;

                        if (_servicioSeleccionadoActual == BorderCampus)
                        {
                            paginaDestino = new PagoExitosoEntregaPage(pinParaEnviar);
                        }
                        else // Retiro
                        {
                            paginaDestino = new PagoExitosoRetiroPage(pinParaEnviar);
                        }

                        await Navigation.PushModalAsync(paginaDestino);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Restaurar botón si seguimos en la misma página
                if (Application.Current.MainPage is not NavigationPage nav || nav.CurrentPage is PagoPage)
                {
                    BtnPagar.Text = textoOriginal;
                    BtnPagar.IsEnabled = true;
                    SpinnerCarga.IsVisible = false;
                    SpinnerCarga.IsRunning = false;
                }
            }
        }

        // --- MÉTODOS VISUALES Y HELPERS ---

        private void SetupInitialServicios()
        {
            var campus = this.FindByName<Border>("BorderCampus");
            var retiro = this.FindByName<Border>("BorderRetiro");
            var consumo = this.FindByName<Border>("BorderConsumo");

            ResetServicioVisuals(campus);
            ResetServicioVisuals(retiro);
            ResetServicioVisuals(consumo);

            // Seleccionar Campus por defecto
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

        private void ResetServicioVisuals(Border b)
        {
            if (b == null) return;
            b.Stroke = new SolidColorBrush(Color.FromArgb("#E0E0E0"));
            b.StrokeThickness = 0; // Sin borde visible cuando no está seleccionado
            b.BackgroundColor = Colors.White;
        }

        private void ApplyServicioSelectedVisuals(Border b)
        {
            if (b == null) return;
            var red = GetAppColor("AlVueloRed", Colors.Red);
            b.Stroke = new SolidColorBrush(red);
            b.StrokeThickness = 1; // Borde rojo visible cuando está seleccionado
            b.BackgroundColor = Colors.White;
        }

        private Color GetAppColor(string key, Color fallback)
        {
            var appResources = Application.Current?.Resources;
            if (appResources != null && appResources.ContainsKey(key) && appResources[key] is Color c)
                return c;
            return fallback;
        }

        private async void OnServicioSelected(object sender, EventArgs e)
        {
            Border border = sender as Border;
            if (border == null) return;

            // Efecto visual rápido
            var originalColor = border.BackgroundColor;
            border.BackgroundColor = Color.FromArgb("#F0F0F0");
            await Task.Delay(100);
            border.BackgroundColor = originalColor;

            // Cambiar selección visual
            if (_servicioSeleccionadoActual != null && _servicioSeleccionadoActual != border)
            {
                ResetServicioVisuals(_servicioSeleccionadoActual);
            }

            ApplyServicioSelectedVisuals(border);
            _servicioSeleccionadoActual = border;

            // Actualizar lógica dependiente
            UpdatePaymentMethodsAvailability();
            UpdateEnvioAmountForService(); // Recalcular total

            await border.ScaleTo(0.98, 50);
            await border.ScaleTo(1.0, 50);
        }

        private void ResetMetodoPagoVisuals(VisualElement element)
        {
            var textSecondary = GetAppColor("TextSecondary", Colors.Gray);
            SetMetodoVisuals(element, textSecondary, Colors.Transparent);
        }

        private void ApplyMetodoPagoSelectedVisuals(VisualElement element)
        {
            var red = GetAppColor("AlVueloRed", Colors.Grey);
            SetMetodoVisuals(element, red, red);
        }

        private void SetMetodoVisuals(VisualElement element, Color textColor, Color borderIconColor)
        {
            if (element == GridMetodo_Tarjeta)
            {
                LabelNombreTarjeta.TextColor = textColor;
                BorderIcon_Tarjeta.Stroke = borderIconColor;
            }
            else if (element == GridMetodo_Deuna)
            {
                LabelDeuna.TextColor = textColor;
                BorderIcon_Deuna.Stroke = borderIconColor;
            }
            else if (element == GridMetodo_Transfer)
            {
                LabelTransfer.TextColor = textColor;
                BorderIcon_Transfer.Stroke = borderIconColor;
            }
            else if (element == GridMetodo_Efectivo)
            {
                LabelEfectivo.TextColor = textColor;
                BorderIcon_Efectivo.Stroke = borderIconColor;
            }
            else if (element == GridMetodo_Agregar)
            {
                LabelAgregar.TextColor = textColor;
                BorderIcon_Agregar.Stroke = borderIconColor;
            }
        }

        private void UncheckRadioOfElement(VisualElement element)
        {
            if (element == GridMetodo_Tarjeta && RadioTarjeta != null) RadioTarjeta.IsChecked = false;
            else if (element == GridMetodo_Deuna && RadioDeuna != null) RadioDeuna.IsChecked = false;
            else if (element == GridMetodo_Transfer && RadioTransfer != null) RadioTransfer.IsChecked = false;
            else if (element == GridMetodo_Efectivo && RadioEfectivo != null) RadioEfectivo.IsChecked = false;
        }

        private void CheckRadioOfElement(VisualElement element)
        {
            if (element == GridMetodo_Tarjeta && RadioTarjeta != null) RadioTarjeta.IsChecked = true;
            else if (element == GridMetodo_Deuna && RadioDeuna != null) RadioDeuna.IsChecked = true;
            else if (element == GridMetodo_Transfer && RadioTransfer != null) RadioTransfer.IsChecked = true;
            else if (element == GridMetodo_Efectivo && RadioEfectivo != null) RadioEfectivo.IsChecked = true;
        }

        private void OnRadioMetodoCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return; // Solo actuar cuando se selecciona (True)

            var rb = sender as RadioButton;
            if (rb == null) return;

            if (RadioTarjeta != null && rb == RadioTarjeta)
            {
                OnMetodoPagoSelected(GridMetodo_Tarjeta, EventArgs.Empty);
            }
            else if (RadioDeuna != null && rb == RadioDeuna && RadioDeuna.IsEnabled)
            {
                OnMetodoPagoSelected(GridMetodo_Deuna, EventArgs.Empty);
            }
            else if (RadioTransfer != null && rb == RadioTransfer && RadioTransfer.IsEnabled)
            {
                OnMetodoPagoSelected(GridMetodo_Transfer, EventArgs.Empty);
            }
            else if (RadioEfectivo != null && rb == RadioEfectivo)
            {
                OnMetodoPagoSelected(GridMetodo_Efectivo, EventArgs.Empty);
            }
        }

        private void UpdatePaymentMethodsAvailability()
        {
            // Solo permitir Deuna/Transferencia si NO es entrega a domicilio
            bool allowDeunaTransfer = _servicioSeleccionadoActual == BorderRetiro || _servicioSeleccionadoActual == BorderConsumo;

            if (RadioDeuna != null) RadioDeuna.IsEnabled = allowDeunaTransfer;
            var gDeuna = this.FindByName<Grid>("GridMetodo_Deuna");
            if (gDeuna != null) gDeuna.Opacity = allowDeunaTransfer ? 1.0 : 0.5;

            if (RadioTransfer != null) RadioTransfer.IsEnabled = allowDeunaTransfer;
            var gTransfer = this.FindByName<Grid>("GridMetodo_Transfer");
            if (gTransfer != null) gTransfer.Opacity = allowDeunaTransfer ? 1.0 : 0.5;

            // Si la selección actual queda deshabilitada, volver a Tarjeta
            if (!allowDeunaTransfer && (_metodoPagoSeleccionadoActual == GridMetodo_Deuna || _metodoPagoSeleccionadoActual == GridMetodo_Transfer))
            {
                ResetMetodoPagoVisuals(_metodoPagoSeleccionadoActual);
                UncheckRadioOfElement(_metodoPagoSeleccionadoActual);
                _metodoPagoSeleccionadoActual = null;

                // Volver a seleccionar Tarjeta por defecto
                OnMetodoPagoSelected(GridMetodo_Tarjeta, EventArgs.Empty);
            }
        }

        private async Task<Tarjeta> ObtenerTarjetaParaPago(string clienteId)
        {
            using (var db = new AlVueloDbContext())
            {
                return await (from ct in db.Set<ClienteTarjeta>()
                              join t in db.Tarjetas on ct.NumTarjeta equals t.NumTarjeta
                              where ct.ClienteId == clienteId
                              select t).FirstOrDefaultAsync();
            }
        }

        private async Task<string> RegistrarPedidoEnBD(string estadoFinal, decimal totalReal)
        {
            try
            {
                using (var db = new AlVueloDbContext())
                {
                    var random = new Random();
                    string pinGenerado = random.Next(0, 10000).ToString("D4");

                    // Determinar el string correcto para el Tipo de Servicio
                    string nombreServicio = "Desconocido";

                    if (_servicioSeleccionadoActual == BorderCampus)
                    {
                        nombreServicio = $"Entrega campus ({PickerCampus.SelectedItem})";
                    }
                    else if (_servicioSeleccionadoActual == BorderRetiro)
                    {
                        nombreServicio = "Retiro en Restaurante";
                    }
                    else if (_servicioSeleccionadoActual == BorderConsumo)
                    {
                        nombreServicio = "Consumo en Restaurante";
                    }

                    var nuevoPedido = new Pedido
                    {
                        ClienteId = "C001",
                        RestauranteId = "UPO1",
                        Total = totalReal,
                        Estado = "Pendiente",
                        TipoServicio = nombreServicio, // Usamos la variable calculada arriba
                        MetodoPago = estadoFinal,
                        Pin = pinGenerado
                    };

                    db.Pedidos.Add(nuevoPedido);
                    await db.SaveChangesAsync();

                    return pinGenerado;
                }
            }
            catch
            {
                return "0000";
            }
        }
    }
}