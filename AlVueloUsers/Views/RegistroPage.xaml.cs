using Alvueloapp.Models;
using AlVueloUsers.Data;
using AlVueloUsers.Models;
using Microsoft.EntityFrameworkCore;

namespace AlVueloUsers.Views;

public partial class RegistroPage : ContentPage
{
    private readonly AlVueloDbContext _db;

    public RegistroPage(AlVueloDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void OnContinuarClicked(object sender, EventArgs e)
    {
        string id = txtId.Text?.Trim() ?? "";
        string correo = txtCorreo.Text?.Trim() ?? "";
        string pass = txtPassword.Text ?? "";
        string celular = txtCelular.Text?.Trim() ?? "";

        try
        {
            if (await _db.Clientes.AnyAsync(x => x.IdBanner == id || x.Correo == correo))
            {
                await DisplayAlert("Error", "Usuario ya existe", "OK");
                return;
            }

            Cliente nuevo = new Cliente
            {
                Id = id, // CHAR(10)
                IdBanner = id, // CHAR(7) - Asegúrate que txtId tenga 7 caracteres
                Nombre = "Usuario Nuevo",
                Correo = correo,
                Password = pass,
                Telefono = celular
            };

            _db.Clientes.Add(nuevo);
            await _db.SaveChangesAsync();
            await DisplayAlert("Éxito", "Cuenta creada", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de DB", ex.InnerException?.Message ?? ex.Message, "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}