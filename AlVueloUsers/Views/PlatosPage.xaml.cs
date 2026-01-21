using AlVueloUsers.Data;
using AlVueloUsers.Models;
using AlVueloUsers.Services;
using Microsoft.EntityFrameworkCore;

namespace AlVueloUsers.Views;

public partial class PlatosPage : ContentPage
{
    private readonly string _restauranteId;

    private List<Plato> _platosTodos = new();
    private List<string> _categorias = new();

    private string _categoriaSeleccionada = "";
    private string _textoBusqueda = "";

    public PlatosPage(string restauranteId)
    {
        InitializeComponent();
        _restauranteId = restauranteId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await CargarHeaderRestaurante();
            await CargarCategoriasYPlatos();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task CargarHeaderRestaurante()
    {
        using var db = new AlVueloDbContext();

        var rest = await db.Restaurantes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == _restauranteId);

        if (rest == null)
        {
            await DisplayAlert("Aviso", "No se encontró el restaurante.", "OK");
            return;
        }

        LblNombreRest.Text = (rest.Nombre ?? "").ToUpper();
        LblCampus.Text = $"Campus: {rest.Campus}";
        LblHorario.Text = $"Horario: {rest.Horario}";
        ImgLogoRest.Source = rest.LogoUrl;
    }

    private async Task CargarCategoriasYPlatos()
    {
        using var db = new AlVueloDbContext();

        _categorias = await db.Menus
            .AsNoTracking()
            .Where(m => m.RestauranteId == _restauranteId)
            .OrderBy(m => m.Id)
            .Select(m => m.Categoria)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToListAsync();

        ListaCategorias.ItemsSource = _categorias;

        _platosTodos = await (from p in db.Platos.AsNoTracking()
                              join m in db.Menus.AsNoTracking() on p.MenuId equals m.Id
                              where p.Disponibilidad
                                    && m.RestauranteId == _restauranteId
                              orderby p.Nombre
                              select new Plato
                              {
                                  Id = p.Id,
                                  Nombre = p.Nombre,
                                  Ingredientes = p.Ingredientes,
                                  Precio = p.Precio,
                                  ImagenUrl = p.ImagenUrl,
                                  Menu = m
                              }).ToListAsync();

        if (_platosTodos.Count == 0)
        {
            await DisplayAlert("Aviso", "No hay platos disponibles para este restaurante.", "OK");
        }

        if (_categorias.Count > 0)
        {
            _categoriaSeleccionada = _categorias[0];

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ListaCategorias.SelectedItem = _categoriaSeleccionada;
                AplicarFiltros();
            });
        }
        else
        {
            _categoriaSeleccionada = ""; // sin categoría => muestra todo
            AplicarFiltros();
        }
    }

    private void AplicarFiltros()
    {
        IEnumerable<Plato> query = _platosTodos;

        if (!string.IsNullOrWhiteSpace(_categoriaSeleccionada))
        {
            query = query.Where(p => (p.Menu?.Categoria ?? "") == _categoriaSeleccionada);
        }

        if (!string.IsNullOrWhiteSpace(_textoBusqueda))
        {
            var t = _textoBusqueda.Trim().ToLowerInvariant();

            query = query.Where(p =>
                (p.Nombre ?? "").ToLowerInvariant().Contains(t) ||
                (p.Ingredientes ?? "").ToLowerInvariant().Contains(t));
        }

        ListaPlatos.ItemsSource = query.ToList();
    }

    private void OnBuscarChanged(object sender, TextChangedEventArgs e)
    {
        _textoBusqueda = e.NewTextValue ?? "";
        AplicarFiltros();
    }

    private void OnCategoriaTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string cat || string.IsNullOrWhiteSpace(cat))
            return;

        _categoriaSeleccionada = cat;

        // esto fuerza el visual Selected
        ListaCategorias.SelectedItem = cat;

        AplicarFiltros();
    }

    private async void OnAgregarPlatoTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Plato plato) return;

        CarritoService.Instancia.Add(plato);
        await DisplayAlert("Carrito", $"{plato.Nombre} agregado", "OK");
    }

    private async void OnBackTapped(object sender, EventArgs e) =>
        await Navigation.PopAsync();

    private async void OnHomeTapped(object sender, EventArgs e) =>
        await Navigation.PopToRootAsync();

    private async void OnCarritoTapped(object sender, EventArgs e) =>
        await Navigation.PushAsync(new CarritoPage());
}
