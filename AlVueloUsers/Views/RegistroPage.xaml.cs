using Alvueloapp.Models;
using AlVueloUsers.Data;
using AlVueloUsers.Models;
using Microsoft.EntityFrameworkCore;

namespace AlVueloUsers.Views;

public partial class RegistroPage : ContentPage
{
    private readonly AlVueloDbContext _db;
    private bool _isPasswordVisible = false;
    private bool _isConfirmPasswordVisible = false;

    public RegistroPage(AlVueloDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // 1. Captura y limpieza de datos
        string identificacion = txtIdentificacion.Text?.Trim() ?? "";
        string idBanner = txtIdBanner.Text?.Trim() ?? "";
        string nombre = txtNombre.Text?.Trim() ?? "";
        string correo = txtCorreo.Text?.Trim().ToLower() ?? "";
        string telefono = txtTelefono.Text?.Trim() ?? "";
        string password = txtPassword.Text ?? "";
        string confirmPassword = txtConfirmPassword.Text ?? "";

        // 2. Validaciones de campos vacíos
        if (string.IsNullOrEmpty(identificacion))
        {
            await MostrarError("Por favor ingresa tu identificación");
            txtIdentificacion.Focus();
            return;
        }

        if (string.IsNullOrEmpty(idBanner))
        {
            await MostrarError("Por favor ingresa tu ID Banner");
            txtIdBanner.Focus();
            return;
        }

        if (string.IsNullOrEmpty(nombre))
        {
            await MostrarError("Por favor ingresa tu nombre completo");
            txtNombre.Focus();
            return;
        }

        if (string.IsNullOrEmpty(correo))
        {
            await MostrarError("Por favor ingresa tu correo institucional");
            txtCorreo.Focus();
            return;
        }

        if (string.IsNullOrEmpty(telefono))
        {
            await MostrarError("Por favor ingresa tu teléfono");
            txtTelefono.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            await MostrarError("Por favor ingresa una contraseña");
            txtPassword.Focus();
            return;
        }

        if (string.IsNullOrEmpty(confirmPassword))
        {
            await MostrarError("Por favor confirma tu contraseña");
            txtConfirmPassword.Focus();
            return;
        }

        // 3. Validaciones de formato
        if (!correo.EndsWith("@udla.edu.ec"))
        {
            await MostrarError("Solo se permiten correos institucionales UDLA (@udla.edu.ec)");
            txtCorreo.Focus();
            return;
        }

        if (!EsEmailValido(correo))
        {
            await MostrarError("Por favor ingresa un correo válido");
            txtCorreo.Focus();
            return;
        }

        // 4. Validación de identificación (10 dígitos)
        if (identificacion.Length != 10 || !identificacion.All(char.IsDigit))
        {
            await MostrarError("La identificación debe tener 10 dígitos");
            txtIdentificacion.Focus();
            return;
        }

        // 5. Validación de ID Banner (7 caracteres)
        if (idBanner.Length != 7)
        {
            await MostrarError("El ID Banner debe tener 7 caracteres");
            txtIdBanner.Focus();
            return;
        }

        // 6. Validación de teléfono (10 dígitos)
        if (telefono.Length != 10 || !telefono.All(char.IsDigit))
        {
            await MostrarError("El teléfono debe tener 10 dígitos");
            txtTelefono.Focus();
            return;
        }

        // 7. Validación de contraseña (6-25 caracteres según SQL)
        if (password.Length < 6 || password.Length > 25)
        {
            await MostrarError("La contraseña debe tener entre 6 y 25 caracteres");
            txtPassword.Focus();
            return;
        }

        // 8. Validación de coincidencia de contraseñas
        if (password != confirmPassword)
        {
            await MostrarError("Las contraseñas no coinciden");
            txtConfirmPassword.Focus();
            await ShakeView(txtPassword);
            await ShakeView(txtConfirmPassword);
            return;
        }

        // 9. Mostrar loading
        btnRegister.IsEnabled = false;
        var textoOriginal = btnRegister.Text;
        btnRegister.Text = "Creando cuenta...";

        // Animación
        await btnRegister.ScaleTo(0.95, 100);
        await btnRegister.ScaleTo(1, 100);

        try
        {
            // 10. Validación de duplicados en la base de datos
            bool existe = await _db.Clientes.AnyAsync(x =>
                x.Correo == correo ||
                x.IdBanner == idBanner ||
                x.Id == identificacion);

            if (existe)
            {
                await MostrarError("El correo, ID Banner o identificación ya están registrados");
                return;
            }

            // 11. Creación del objeto Cliente
            Cliente nuevoCliente = new Cliente
            {
                Id = identificacion,
                IdBanner = idBanner,
                Nombre = nombre,
                Correo = correo,
                Password = password,
                Telefono = telefono,
                Imagen_Url = "" // URL vacía por defecto
            };

            // 12. Guardado en SQL Server
            _db.Clientes.Add(nuevoCliente);
            await _db.SaveChangesAsync();

            // 13. Mostrar éxito
            btnRegister.Text = "✓ ¡Cuenta creada!";
            btnRegister.BackgroundColor = Colors.Green;

            await DisplayAlert(
                "¡Bienvenido!",
                $"Tu cuenta ha sido creada exitosamente, {nombre}. Ya puedes iniciar sesión.",
                "Continuar");

            // 14. Navegar a LoginPage
            await Shell.Current.GoToAsync("///LoginPage");
        }
        catch (DbUpdateException ex)
        {
            // Errores específicos de SQL (truncamiento, llaves duplicadas, etc.)
            var mensaje = ex.InnerException?.Message ?? ex.Message;

            if (mensaje.Contains("duplicate") || mensaje.Contains("UNIQUE"))
            {
                await MostrarError("Ya existe un usuario con estos datos");
            }
            else if (mensaje.Contains("String or binary data would be truncated"))
            {
                await MostrarError("Uno o más campos exceden el tamaño permitido");
            }
            else
            {
                await MostrarError($"Error de base de datos: {mensaje}");
            }
        }
        catch (Exception ex)
        {
            await MostrarError($"Ocurrió un error inesperado: {ex.Message}");
        }
        finally
        {
            // Restaurar el botón
            btnRegister.Text = textoOriginal;
            btnRegister.IsEnabled = true;
            btnRegister.BackgroundColor = Color.FromArgb("#4E222A");
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Volver a LoginPage
        await Shell.Current.GoToAsync("///LoginPage");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // Volver atrás
        await Shell.Current.GoToAsync("///LoginPage");
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        txtPassword.IsPassword = !_isPasswordVisible;

        var button = (ImageButton)sender;
        button.Source = _isPasswordVisible ? "eye_off_icon.png" : "eye_icon.png";
    }

    private void OnToggleConfirmPasswordClicked(object sender, EventArgs e)
    {
        _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
        txtConfirmPassword.IsPassword = !_isConfirmPasswordVisible;

        var button = (ImageButton)sender;
        button.Source = _isConfirmPasswordVisible ? "eye_off_icon.png" : "eye_icon.png";
    }

    // Métodos auxiliares
    private bool EsEmailValido(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task MostrarError(string mensaje)
    {
        await DisplayAlert("Error", mensaje, "OK");
    }

    private async Task ShakeView(VisualElement view)
    {
        // Animación de shake para errores
        for (int i = 0; i < 4; i++)
        {
            await view.TranslateTo(-10, 0, 50);
            await view.TranslateTo(10, 0, 50);
        }
        await view.TranslateTo(0, 0, 50);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Reset de campos
        txtPassword.IsPassword = true;
        txtConfirmPassword.IsPassword = true;
        _isPasswordVisible = false;
        _isConfirmPasswordVisible = false;
    }
}