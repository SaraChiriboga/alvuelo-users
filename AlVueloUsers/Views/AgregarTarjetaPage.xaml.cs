using AlVueloUsers.Data;
using AlVueloUsers.Models;
using AlVueloUsers.Services;
using Microsoft.EntityFrameworkCore;
#if ANDROID
using Android.Graphics.Drawables;
#endif

namespace AlVueloUsers.Views
{
    public partial class AgregarTarjetaPage : ContentPage
    {
        private readonly PayPalService _paypalService;
        private string _tipoTarjeta = "Desconocida";
        private decimal _montoAPagar;
        private string _clienteId;
        private bool _isFormatting = false;

        public AgregarTarjetaPage()
        {
            InitializeComponent();
            _paypalService = new PayPalService();
            _clienteId = "C001";
            ModifyEntryHandler();
        }

        public AgregarTarjetaPage(decimal montoAPagar, string clienteId) : this()
        {
            _montoAPagar = montoAPagar; // Guardamos el valor dinámico (ej: 7.54, 8.04, etc.)
            _clienteId = clienteId;
            BtnPagar.Text = $"Pagar ${_montoAPagar:F2}";
        }

        void ModifyEntryHandler()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
            {
#if ANDROID
                if (h.PlatformView != null)
                    h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
        }

        // --- EVENTOS UI (Idénticos pero seguros) ---
        private void OnNumeroTarjetaChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            if (e.NewTextValue == e.OldTextValue) return;
            Dispatcher.Dispatch(() => {
                try
                {
                    _isFormatting = true;
                    string val = e.NewTextValue ?? "";
                    string numeroLimpio = val.Replace(" ", "");
                    if (numeroLimpio.Length > 16) numeroLimpio = numeroLimpio.Substring(0, 16);

                    string formatted = "";
                    for (int i = 0; i < numeroLimpio.Length; i++)
                    {
                        if (i > 0 && i % 4 == 0) formatted += " ";
                        formatted += numeroLimpio[i];
                    }
                    if (EntryNumeroTarjeta.Text != formatted)
                    {
                        EntryNumeroTarjeta.Text = formatted;
                        try { EntryNumeroTarjeta.CursorPosition = formatted.Length; } catch { }
                    }
                    ActualizarPreviewTarjeta(numeroLimpio);
                    DetectarTipoTarjeta(numeroLimpio);
                }
                finally { _isFormatting = false; }
            });
        }

        private void OnFechaExpiracionChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            if (e.NewTextValue == e.OldTextValue) return;
            Dispatcher.Dispatch(() => {
                try
                {
                    _isFormatting = true;
                    string val = e.NewTextValue ?? "";
                    string fechaLimpia = val.Replace("/", "");
                    if (fechaLimpia.Length > 4) fechaLimpia = fechaLimpia.Substring(0, 4);

                    string formatted = fechaLimpia;
                    if (fechaLimpia.Length >= 2) formatted = fechaLimpia.Insert(2, "/");
                    if (EntryFechaExpiracion.Text != formatted)
                    {
                        EntryFechaExpiracion.Text = formatted;
                        try { EntryFechaExpiracion.CursorPosition = formatted.Length; } catch { }
                    }
                    FechaTarjetaPreview.Text = string.IsNullOrEmpty(formatted) ? "MM/AA" : formatted;
                }
                finally { _isFormatting = false; }
            });
        }

        private void OnNombreTitularChanged(object sender, TextChangedEventArgs e)
        {
            string val = e.NewTextValue ?? "";
            NombreTarjetaPreview.Text = string.IsNullOrEmpty(val) ? "TU NOMBRE" : val.ToUpper();
        }

        // --- LÓGICA VISUAL ---
        private void ActualizarPreviewTarjeta(string numeroLimpio)
        {
            string num = numeroLimpio ?? "";
            if (string.IsNullOrEmpty(num)) NumeroTarjetaPreview.Text = "•••• •••• •••• ••••";
            else
            {
                string preview = "";
                for (int i = 0; i < 16; i++)
                {
                    if (i > 0 && i % 4 == 0) preview += " ";
                    if (i < num.Length) preview += num[i];
                    else preview += "•";
                }
                NumeroTarjetaPreview.Text = preview;
            }
        }

        private void DetectarTipoTarjeta(string numero)
        {
            string num = numero ?? "";
            if (string.IsNullOrEmpty(num)) { ResetearTipoTarjeta(); return; }
            Color colorFondo = Color.FromArgb("#CCCCCC");
            string iconoGlyph = "\uf1f0";
            bool esConocida = true;
            if (num.StartsWith("4")) { _tipoTarjeta = "Visa"; iconoGlyph = "\uf1f0"; colorFondo = Color.FromArgb("#1A1F71"); }
            else if (num.StartsWith("5")) { _tipoTarjeta = "Mastercard"; iconoGlyph = "\uf1f1"; colorFondo = Color.FromArgb("#EB001B"); }
            else if (num.StartsWith("3")) { _tipoTarjeta = "Amex"; iconoGlyph = "\uf1f3"; colorFondo = Color.FromArgb("#006FCF"); }
            else { esConocida = false; }
            if (!esConocida) { ResetearTipoTarjeta(); return; }
            ActualizarLogoTarjeta(iconoGlyph, colorFondo, Colors.White);
            IconoTipoTarjeta.IsVisible = true;
            if (IconoTipoTarjeta.Source is FontImageSource fontIcon) { fontIcon.Glyph = iconoGlyph; fontIcon.Color = colorFondo; }
            else { IconoTipoTarjeta.Source = new FontImageSource { FontFamily = "FABrands", Glyph = iconoGlyph, Color = colorFondo, Size = 32 }; }
        }

        private void ActualizarLogoTarjeta(string glyph, Color colorFondo, Color colorLogo)
        {
            if (LogoTarjeta.Source is FontImageSource fontImage) { fontImage.Glyph = glyph; fontImage.Color = colorLogo; }
            else { LogoTarjeta.Source = new FontImageSource { FontFamily = "FABrands", Glyph = glyph, Color = colorLogo, Size = 40 }; }
            TarjetaPreview.BackgroundColor = colorFondo;
        }

        private void ResetearTipoTarjeta()
        {
            _tipoTarjeta = "Desconocida";
            ActualizarLogoTarjeta("\uf1f0", Color.FromArgb("#1a1a1a"), Colors.Transparent);
            IconoTipoTarjeta.IsVisible = false;
        }

        // --- LÓGICA DEL FLOATING BADGE (NUEVO) ---
        private async void MostrarErrorBadge(string mensaje)
        {
            LabelMensajeError.Text = mensaje;
            BadgeError.IsVisible = true;
            BadgeError.TranslationY = 15;

            // Animación de entrada
            await Task.WhenAll(
                BadgeError.FadeTo(1, 250, Easing.CubicOut),
                BadgeError.TranslateTo(0, 0, 250, Easing.CubicOut)
            );

            await Task.Delay(3500);

            // Animación de salida
            await BadgeError.FadeTo(0, 300, Easing.CubicIn);
            BadgeError.IsVisible = false;
        }

        private async void OnPagarClicked(object sender, EventArgs e)
        {
            if (!ValidarCampos())
            {
                MostrarErrorBadge("Por favor, completa todos los campos correctamente.");
                return;
            }

            string textoOriginal = BtnPagar.Text ?? "Pagar";

            try
            {
                // UI: Bloquear y cargar
                BtnPagar.Text = "";
                BtnPagar.IsEnabled = false;
                SpinnerCarga.IsVisible = true;
                SpinnerCarga.IsRunning = true;

                // RECOPILACIÓN SEGURA DE DATOS
                string numeroLimpio = (EntryNumeroTarjeta.Text ?? "").Replace(" ", "");
                string titular = (EntryNombreTitular.Text ?? "").ToUpper();
                string fecha = EntryFechaExpiracion.Text ?? "";
                string cvv = EntryCvv.Text ?? "";

                // 1. GUARDAR EN BD (Si el usuario lo solicitó)
                string tarjetaGuardadaNum = null;
                if (SwitchGuardarTarjeta.IsToggled)
                {
                    tarjetaGuardadaNum = await GuardarTarjetaEnBD(numeroLimpio, titular, fecha, cvv);
                }

                // 2. PROCESAR CON PAYPAL API
                var resultadoPago = await _paypalService.ProcesarPagoConTarjeta(
                    _montoAPagar, numeroLimpio, titular, fecha, cvv, _tipoTarjeta
                );

                if (resultadoPago.Exito)
                {
                    string pinGenerado = await CrearPedido(numeroLimpio); // Usamos el número real del pago

                    // Navegación exitosa (Modal)
                    await Navigation.PushModalAsync(new PagoExitosoEntregaPage(pinGenerado));
                    return;
                }
                else
                {
                    // En lugar de alerta, mostramos el Badge con el error de PayPal
                    MostrarErrorBadge(resultadoPago.Mensaje);
                }
            }
            catch (Exception ex)
            {
                MostrarErrorBadge("Error inesperado: " + ex.Message);
            }
            finally
            {
                // Solo restauramos si la página sigue activa
                if (this.IsLoaded)
                {
                    BtnPagar.Text = textoOriginal;
                    BtnPagar.IsEnabled = true;
                    SpinnerCarga.IsVisible = false;
                    SpinnerCarga.IsRunning = false;
                }
            }
        }
        private bool ValidarCampos()
        {
            if (string.IsNullOrEmpty(EntryNumeroTarjeta.Text) || EntryNumeroTarjeta.Text.Length < 16) return false;
            if (string.IsNullOrEmpty(EntryNombreTitular.Text)) return false;
            if (string.IsNullOrEmpty(EntryCvv.Text) || EntryCvv.Text.Length < 3) return false;
            if (string.IsNullOrEmpty(EntryFechaExpiracion.Text)) return false;
            return true;
        }

        // --- MÉTODO BD QUE NO FALLA SI YA EXISTE ---
        private async Task<string> GuardarTarjetaEnBD(string num, string nombre, string fecha, string cvv)
        {
            // 1. Intentar Guardar Tarjeta
            try
            {
                using (var db = new AlVueloDbContext())
                {
                    // Si no existe, la agregamos
                    if (!await db.Tarjetas.AnyAsync(t => t.NumTarjeta == num))
                    {
                        var tarjeta = new Tarjeta { NumTarjeta = num, NombreTitular = nombre, FechaExpiracion = fecha, Cvv = cvv };
                        db.Tarjetas.Add(tarjeta);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                // Si falla (ej. carrera de hilos o duplicado), lo ignoramos. 
                // Asumimos que la tarjeta ya está ahí.
            }

            // 2. Intentar Guardar Relación (En un contexto nuevo y limpio)
            try
            {
                using (var db = new AlVueloDbContext())
                {
                    // Si no existe la relación, la agregamos
                    if (!await db.ClientesTarjetas.AnyAsync(ct => ct.ClienteId == _clienteId && ct.NumTarjeta == num))
                    {
                        db.ClientesTarjetas.Add(new ClienteTarjeta { ClienteId = _clienteId, NumTarjeta = num });
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                // Ignorar error de relación duplicada
            }

            return num;
        }

        private async Task<string> CrearPedido(string tarjetaNum)
        {
            try
            {
                using (var db = new AlVueloDbContext())
                {
                    string pinGenerado = new Random().Next(0, 10000).ToString("D4");
                    var pedido = new Pedido
                    {
                        ClienteId = _clienteId,
                        RestauranteId = "UPO1",
                        Estado = "Confirmado",
                        TipoServicio = "Entrega (Nueva Tarjeta)",
                        MetodoPago = $"Tarjeta {_tipoTarjeta}",
                        Pin = pinGenerado,
                        Total = _montoAPagar
                    };
                    db.Pedidos.Add(pedido);
                    await db.SaveChangesAsync();
                    return pinGenerado;
                }
            }
            catch { return "0000"; }
        }

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}