using AlVueloUsers.Data;
using AlVueloUsers.Views;
using Microsoft.EntityFrameworkCore;

namespace AlVueloUsers.Views;

public partial class LoginPage : ContentPage
{
    private readonly AlVueloDbContext _db;
    private bool _isPasswordVisible = false;

    // 1. Constructor con Inyección de Dependencias
    public LoginPage(AlVueloDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    // 2. Lógica de Inicio de Sesión Real
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Validaciones de campos vacíos
        if (string.IsNullOrWhiteSpace(txtCorreo.Text))
        {
            await DisplayAlert("Error", "Por favor ingresa tu correo electrónico", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            await DisplayAlert("Error", "Por favor ingresa tu contraseña", "OK");
            return;
        }

        // Validación de formato de correo
        if (!EsEmailValido(txtCorreo.Text))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo válido", "OK");
            return;
        }

        // Mostrar indicador de carga y deshabilitar botón
        btnLogin.IsEnabled = false;
        var textoOriginal = btnLogin.Text;
        btnLogin.Text = "Verificando...";

        // Animación de presión en el botón
        await btnLogin.ScaleTo(0.95, 100);
        await btnLogin.ScaleTo(1, 100);

        try
        {
            string correo = txtCorreo.Text.Trim().ToLower();
            string password = txtPassword.Text;

            // COMPROBACIÓN REAL: Buscamos en la tabla Cliente (Singular en SQL)
            var usuario = await _db.Clientes
                .FirstOrDefaultAsync(u => u.Correo == correo && u.Password == password);

            if (usuario != null)
            {
                btnLogin.Text = "¡Bienvenido!";
                await Task.Delay(500);

                // Navegar a la página principal
                await Shell.Current.GoToAsync("///restaurantes");
            }
            else
            {
                await DisplayAlert("Acceso Denegado", "Correo o contraseña incorrectos", "OK");
            }
        }
        catch (Exception ex)
        {
            // Error de conexión (Object reference error o Timeout)
            var msg = ex.InnerException?.Message ?? ex.Message;
            await DisplayAlert("Error de Red", $"No se pudo conectar al servidor: {msg}", "OK");
        }
        finally
        {
            // Restaurar estado del botón
            btnLogin.Text = textoOriginal;
            btnLogin.IsEnabled = true;
        }
    }

    // 3. Navegación a Registro (Usando ruta de AppShell)
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Ruta registrada previamente en AppShell.xaml.cs
        await Shell.Current.GoToAsync("RegistroPage");
    }

    // 4. Mostrar / Ocultar Contraseña
    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        txtPassword.IsPassword = !_isPasswordVisible;

        if (sender is ImageButton button)
        {
            button.Source = _isPasswordVisible ? "eye_icon.png" : "eye_off_icon.png";
        }
    }

    // 5. Recuperación de Contraseña
    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        string email = await DisplayPromptAsync(
            "Recuperar Contraseña",
            "Ingresa tu correo institucional:",
            "Enviar",
            "Cancelar",
            "ejemplo@udla.edu.ec",
            keyboard: Keyboard.Email);

        if (!string.IsNullOrWhiteSpace(email) && EsEmailValido(email))
        {
            await DisplayAlert("Soporte", $"Se han enviado instrucciones a {email}", "OK");
        }
    }

    // 6. Métodos de Login Social (Animaciones y Diálogos)
    private async void OnGoogleLoginTapped(object sender, EventArgs e)
    {
        await AnimarBotonSocial(sender);
        await DisplayAlert("Google", "Login con Google próximamente disponible", "OK");
    }

    private async void OnFacebookLoginTapped(object sender, EventArgs e)
    {
        await AnimarBotonSocial(sender);
        await DisplayAlert("Facebook", "Login con Apple próximamente disponible", "OK");
    }

    private async void OnAppleLoginTapped(object sender, EventArgs e)
    {
        await AnimarBotonSocial(sender);
        await DisplayAlert("Apple", "Login con teléfono próximamente disponible", "OK");
    }

    // 7. Métodos Auxiliares (Validaciones y Animaciones)
    private bool EsEmailValido(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch { return false; }
    }

    private async Task AnimarBotonSocial(object sender)
    {
        if (sender is Border border)
        {
            await border.ScaleTo(0.9, 100);
            await border.ScaleTo(1, 100);
        }
    }

    // Limpieza al aparecer la página
    protected override void OnAppearing()
    {
        base.OnAppearing();
        txtPassword.IsPassword = true;
        _isPasswordVisible = false;
    }
}