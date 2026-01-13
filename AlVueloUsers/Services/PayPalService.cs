using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlVueloUsers.Services
{
    public class PayPalService
    {
        // ⚠️ Asegúrate de que este ClientId sea de una cuenta "BUSINESS" en el Sandbox
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
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ClientId}:{Secret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.PostAsync($"{BaseUrl}/v1/oauth2/token", content);
            var json = await response.Content.ReadAsStringAsync();

            // Debug para ver si falla el token
            if (!response.IsSuccessStatusCode) throw new Exception($"Error Token: {json}");

            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        // --- MÉTODO NUEVO NECESARIO ---
        // Este método simula el pago para que AgregarTarjetaPage funcione
        public async Task<bool> ProcesarPagoConTarjeta(decimal monto, string numero, string nombre, string fecha, string cvv, string tipo)
        {
            await Task.Delay(3000); // Simular espera

            // Validación básica simulada
            if (!string.IsNullOrEmpty(numero) && !string.IsNullOrEmpty(cvv))
            {
                return true;
            }
            return false;
        }
        // CAMBIO: Ahora devolvemos una tupla (Exito, MensajeError)
        // Método actualizado con la corrección del Header
        public async Task<(bool Exito, string Mensaje)> ProcesarPago(decimal total, string numeroTarjeta, string fechaExpiracion, string cvv, string titular)
        {
            try
            {
                string token = await GetAccessTokenAsync();

                // Formato fecha: De '12/28' a '2028-12'
                var partes = fechaExpiracion.Split('/');
                string anio = "20" + partes[1];
                string mes = partes[0];

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
                            number = numeroTarjeta,
                            expiry = $"{anio}-{mes}",
                            security_code = cvv,
                            name = titular,
                            // Agregamos atributos básicos de facturación para evitar rechazos por datos incompletos
                            billing_address = new
                            {
                                address_line_1 = "Av. Granados", // Puedes poner datos genéricos o reales del usuario
                                admin_area_2 = "Quito",
                                admin_area_1 = "Pichincha",
                                postal_code = "170125",
                                country_code = "EC"
                            }
                        }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // --- LA SOLUCIÓN ESTÁ AQUÍ ---
                // Generamos un ID único para esta transacción específica
                request.Headers.Add("PayPal-Request-Id", Guid.NewGuid().ToString());
                // -----------------------------

                request.Content = new StringContent(JsonSerializer.Serialize(orden), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Aprobado");
                }
                else
                {
                    // Verás el error detallado si falla
                    System.Diagnostics.Debug.WriteLine($"PAYPAL ERROR: {jsonResponse}");
                    return (false, $"PayPal rechazó: {jsonResponse}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error Técnico: {ex.Message}");
            }
        }
    }
}