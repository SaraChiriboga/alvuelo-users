using AlVueloUsers.Data;
using AlVueloUsers.Models;
using AlVueloUsers.Services;
using Microsoft.EntityFrameworkCore;

namespace AlVueloUsers.Views
{
    public partial class AgregarTarjetaPage : ContentPage
    {
        private readonly PayPalService _paypalService;
        private string _tipoTarjeta = "Desconocida";
        private decimal _montoAPagar;
        private string _clienteId;

        // 1. CONSTRUCTOR VACÍO (Obligatorio para que MAUI no crashee)
        public AgregarTarjetaPage()
        {
            InitializeComponent();
            _paypalService = new PayPalService();
        }

        // 2. CONSTRUCTOR CON PARÁMETROS (El que usas tú manualmente)
        // Usamos ": this()" para reciclar el código del constructor vacío
        public AgregarTarjetaPage(decimal montoAPagar, string clienteId) : this()
        {
            _montoAPagar = montoAPagar;
            _clienteId = clienteId;

            // Actualizar texto del botón con el monto
            BtnPagar.Text = $"Pagar ${_montoAPagar:F2}";
        }

        private void OnNumeroTarjetaChanged(object sender, TextChangedEventArgs e)
        {
            // Evitar bucles infinitos si el texto no cambió realmente
            if (e.NewTextValue == e.OldTextValue) return;

            string textoOriginal = e.NewTextValue ?? "";
            string numeroLimpio = textoOriginal.Replace(" ", "");

            // 1. LIMITAR LONGITUD MANUALMENTE (Reemplazo de MaxLength)
            // 16 dígitos reales máximo
            if (numeroLimpio.Length > 16)
            {
                numeroLimpio = numeroLimpio.Substring(0, 16);
            }

            // 2. FORMATEAR (1234 5678...)
            string formatted = "";
            for (int i = 0; i < numeroLimpio.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    formatted += " ";
                formatted += numeroLimpio[i];
            }

            // 3. APLICAR CAMBIOS SOLO SI ES NECESARIO
            if (EntryNumeroTarjeta.Text != formatted)
            {
                EntryNumeroTarjeta.Text = formatted;

                // TRUCO: Mantener el cursor al final para que no salte al inicio
                // Esto evita que el usuario se frustre al escribir rápido
                EntryNumeroTarjeta.CursorPosition = formatted.Length;
                return;
            }

            // 4. ACTUALIZAR VISTA PREVIA (Lógica visual)
            ActualizarPreviewTarjeta(numeroLimpio);

            // 5. DETECTAR TIPO
            DetectarTipoTarjeta(numeroLimpio);
        }

        // He separado esto en un método pequeño para que el código de arriba sea más limpio
        private void ActualizarPreviewTarjeta(string numeroLimpio)
        {
            if (string.IsNullOrEmpty(numeroLimpio))
            {
                NumeroTarjetaPreview.Text = "•••• •••• •••• ••••";
            }
            else
            {
                string preview = "";
                for (int i = 0; i < 16; i++)
                {
                    if (i > 0 && i % 4 == 0) preview += " ";

                    if (i < numeroLimpio.Length)
                        preview += numeroLimpio[i];
                    else
                        preview += "•";
                }
                NumeroTarjetaPreview.Text = preview;
            }
        }

        private void DetectarTipoTarjeta(string numero)
        {
            if (string.IsNullOrEmpty(numero)) { ResetearTipoTarjeta(); return; }

            string colorFondo = "#1a1a1a";
            string iconoGlyph = "\uf1f0";
            string colorMarca = "#CCCCCC";

            if (numero.StartsWith("4")) { _tipoTarjeta = "Visa"; iconoGlyph = "\uf1f0"; colorMarca = "#1A1F71"; }
            else if (numero.StartsWith("5")) { _tipoTarjeta = "Mastercard"; iconoGlyph = "\uf1f1"; colorMarca = "#EB001B"; }
            else if (numero.StartsWith("3")) { _tipoTarjeta = "Amex"; iconoGlyph = "\uf1f3"; colorMarca = "#006FCF"; }
            else { ResetearTipoTarjeta(); return; }

            ActualizarLogoTarjeta(iconoGlyph, colorMarca);
            IconoTipoTarjeta.IsVisible = true;
            IconoTipoTarjeta.Source = new FontImageSource { FontFamily = "FABrands", Glyph = iconoGlyph, Color = Color.FromArgb(colorMarca), Size = 32 };
        }

        private void ActualizarLogoTarjeta(string glyph, string colorHex)
        {
            LogoTarjeta.Source = new FontImageSource { FontFamily = "FABrands", Glyph = glyph, Color = Color.FromArgb(colorHex), Size = 50 };
            TarjetaPreview.BackgroundColor = Color.FromArgb(colorHex);
        }

        private void ResetearTipoTarjeta()
        {
            _tipoTarjeta = "Desconocida";
            LogoTarjeta.Source = new FontImageSource { FontFamily = "FABrands", Glyph = "\uf1f0", Color = Colors.Gray, Size = 50 };
            IconoTipoTarjeta.IsVisible = false;
            TarjetaPreview.BackgroundColor = Color.FromArgb("#1a1a1a");
        }

        private void OnNombreTitularChanged(object sender, TextChangedEventArgs e)
        {
            string nombre = e.NewTextValue?.ToUpper() ?? "";
            NombreTarjetaPreview.Text = string.IsNullOrEmpty(nombre) ? "TU NOMBRE" : nombre;
        }

        private void OnFechaExpiracionChanged(object sender, TextChangedEventArgs e)
        {
            string fecha = e.NewTextValue?.Replace("/", "") ?? "";
            if (fecha.Length >= 2 && !e.NewTextValue.Contains("/"))
            {
                fecha = fecha.Insert(2, "/");
                EntryFechaExpiracion.Text = fecha;
                return;
            }
            FechaTarjetaPreview.Text = string.IsNullOrEmpty(e.NewTextValue) ? "MM/AA" : e.NewTextValue;
        }

        private async void OnPagarClicked(object sender, EventArgs e)
        {
            if (!ValidarCampos()) return;

            try
            {
                LoadingOverlay.IsVisible = true;
                BtnPagar.IsEnabled = false;

                string tarjetaGuardadaNum = null;
                if (SwitchGuardarTarjeta.IsToggled)
                {
                    tarjetaGuardadaNum = await GuardarTarjetaEnBD();
                }

                string numeroLimpio = EntryNumeroTarjeta.Text.Replace(" ", "");

                // Llama al servicio (que arreglaremos en el paso 2)
                bool pagoExitoso = await _paypalService.ProcesarPagoConTarjeta(
                    _montoAPagar, numeroLimpio, EntryNombreTitular.Text, EntryFechaExpiracion.Text, EntryCvv.Text, _tipoTarjeta
                );

                if (pagoExitoso)
                {
                    string pinGenerado = await CrearPedido(tarjetaGuardadaNum);
                    await DisplayAlert("¡Pago Exitoso!", "Tu pedido ha sido confirmado.", "OK");

                    // Asegúrate de que PagoExitosoEntregaPage tenga el constructor que recibe string
                    Application.Current.MainPage = new NavigationPage(new PagoExitosoEntregaPage(pinGenerado));
                }
                else
                {
                    await DisplayAlert("Error", "El pago fue rechazado.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un problema: {ex.Message}", "OK");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                BtnPagar.IsEnabled = true;
            }
        }

        private bool ValidarCampos()
        {
            // Validaciones simples
            if ((EntryNumeroTarjeta.Text?.Length ?? 0) < 13) return false;
            if (string.IsNullOrWhiteSpace(EntryNombreTitular.Text)) return false;
            if ((EntryCvv.Text?.Length ?? 0) < 3) return false;
            return true;
        }

        private async Task<string> GuardarTarjetaEnBD()
        {
            try
            {
                using (var db = new AlVueloDbContext())
                {
                    string numeroLimpio = EntryNumeroTarjeta.Text.Replace(" ", "");
                    var existe = await db.Tarjetas.AnyAsync(t => t.NumTarjeta == numeroLimpio);

                    if (!existe)
                    {
                        var tarjeta = new Tarjeta
                        {
                            NumTarjeta = numeroLimpio,
                            NombreTitular = EntryNombreTitular.Text,
                            FechaExpiracion = EntryFechaExpiracion.Text,
                            Cvv = EntryCvv.Text
                            // QUITAMOS 'TipoTarjeta' PORQUE NO ESTÁ EN TU MODELO
                        };
                        db.Tarjetas.Add(tarjeta);
                        await db.SaveChangesAsync();
                    }

                    var relacion = new ClienteTarjeta { ClienteId = _clienteId, NumTarjeta = numeroLimpio };
                    if (!await db.ClientesTarjetas.AnyAsync(ct => ct.ClienteId == _clienteId && ct.NumTarjeta == numeroLimpio))
                    {
                        db.ClientesTarjetas.Add(relacion);
                        await db.SaveChangesAsync();
                    }
                    return numeroLimpio;
                }
            }
            catch (Exception) { return null; }
        }

        private async Task<string> CrearPedido(string tarjetaNum)
        {
            using (var db = new AlVueloDbContext())
            {
                string pinGenerado = new Random().Next(0, 10000).ToString("D4");

                var pedido = new Pedido
                {
                    ClienteId = _clienteId,
                    RestauranteId = "R001",
                    Estado = "Confirmado",
                    TipoServicio = "Entrega (Nueva Tarjeta)",
                    MetodoPago = $"Tarjeta {_tipoTarjeta}",
                    Pin = pinGenerado,

                    // CORRECCIÓN DE MODELO:
                    // Usamos 'Total' (que sí existe) en vez de 'MontoTotal'
                    // Quitamos 'FechaHora' (seguramente la BD lo pone por defecto o no existe)
                    Total = _montoAPagar
                };

                db.Pedidos.Add(pedido);
                await db.SaveChangesAsync();

                return pinGenerado;
            }
        }

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}