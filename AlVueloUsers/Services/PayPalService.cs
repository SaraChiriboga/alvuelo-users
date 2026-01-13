using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlVueloUsers.Services
{
    public class PayPalService
    {
        // ⚠️ TUS CREDENCIALES
        private const string ClientId = "AdsfnR5U0ujroMXvrQ9ZIQvbliQE6UL1-B-lXkPw9f2P6hI9Wz7JynpeLvlxl1w7XHLqSJotU7IrC_iu";
        private const string Secret = "EKJEThN-MvhNGwWxyzZwkHum1EF29LhFSpihS-pTQGQeVY8j7Qyx8V0gNuwEIphIqDAMKLEyhmXGwLzC";
        private const string BaseUrl = "https://api-m.sandbox.paypal.com";

        private readonly HttpClient _httpClient;

        public PayPalService()
        {
            _httpClient = new HttpClient();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ClientId}:{Secret}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = await _httpClient.PostAsync($"{BaseUrl}/v1/oauth2/token", content);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("access_token").GetString();
            }
            catch { return null; }
        }

        // --- MÉTODO 1: Para 'AgregarTarjetaPage' ---
        // Simplemente redirige a la lógica real
        public async Task<(bool Exito, string Mensaje)> ProcesarPagoConTarjeta(decimal monto, string numero, string nombre, string fecha, string cvv, string tipo)
        {
            return await ProcesarPago(monto, numero, fecha, cvv, nombre);
        }

        // --- MÉTODO 2: Lógica Real de la API (Corregida) ---
        public async Task<(bool Exito, string Mensaje)> ProcesarPago(decimal total, string numeroTarjeta, string fechaExpiracion, string cvv, string titular)
        {
            try
            {
                string token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return (false, "No se pudo conectar con PayPal (Token).");

                // 1. LIMPIEZA DE DATOS
                string numeroLimpio = numeroTarjeta.Replace(" ", "");

                // 2. CORRECCIÓN DE FECHA (CRÍTICO PARA QUE NO FALLE)
                // PayPal exige AAAA-MM (Ej: 2028-05). Si mandas 2028-5, falla.
                string anio = "2025";
                string mes = "12";

                if (fechaExpiracion.Contains("/"))
                {
                    var partes = fechaExpiracion.Split('/');
                    if (partes.Length == 2)
                    {
                        // PadLeft(2, '0') convierte "5" en "05"
                        mes = partes[0].Trim().PadLeft(2, '0');
                        anio = "20" + partes[1].Trim();
                    }
                }

                // 3. CONSTRUCCIÓN DE LA ORDEN
                var orden = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new {
                            amount = new { currency_code = "USD", value = total.ToString("F2").Replace(",", ".") }
                        }
                    },
                    payment_source = new
                    {
                        card = new
                        {
                            number = numeroLimpio,
                            expiry = $"{anio}-{mes}", // Formato corregido: AAAA-MM
                            security_code = cvv,
                            name = titular.ToUpper(),

                            // 4. DIRECCIÓN SEGURA PARA SANDBOX
                            // Usamos una dirección de EE.UU. para evitar errores de validación regional
                            billing_address = new
                            {
                                address_line_1 = "1 Main St",
                                admin_area_2 = "San Jose",
                                admin_area_1 = "CA",
                                postal_code = "95131",
                                country_code = "US"
                            }
                        }
                    }
                };

                // 5. ENVÍO
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());
                request.Content = new StringContent(JsonSerializer.Serialize(orden), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Aprobado");
                }
                else
                {
                    // Extracción del mensaje de error real
                    string errorMsg = "Rechazado por PayPal";
                    try
                    {
                        using var doc = JsonDocument.Parse(jsonResponse);
                        if (doc.RootElement.TryGetProperty("message", out var msg))
                            errorMsg = msg.GetString();

                        // Si es error de validación, suele venir en "details"
                        if (doc.RootElement.TryGetProperty("details", out var details) && details.GetArrayLength() > 0)
                        {
                            var issue = details[0].GetProperty("issue").GetString();
                            errorMsg += $": {issue}";
                        }
                    }
                    catch { }

                    // Log para que veas qué pasó en la consola de salida
                    System.Diagnostics.Debug.WriteLine($"PAYPAL ERROR: {jsonResponse}");

                    return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error Técnico: {ex.Message}");
            }
        }
    }
}