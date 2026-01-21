using AlVueloUsers.Data;
using AlVueloUsers.Models;

namespace AlVueloUsers.Views
{
    public partial class RestaurantesPage : ContentPage
    {
        private List<Restaurante> _restaurantesBase = new();

        public RestaurantesPage()
        {
            InitializeComponent();

            PickerCampus.ItemsSource = new List<string> { "UdlaPark", "Granados", "Colón" };
            PickerCampus.SelectedIndex = 0; // default

            CargarRestaurantes();
        }

        private void CargarRestaurantes()
        {
            try
            {
                string campus = (PickerCampus.SelectedItem?.ToString()) ?? "UdlaPark";

                using (var db = new AlVueloDbContext())
                {
                    _restaurantesBase = db.Restaurantes
                        .Where(r => r.Activo && r.Campus == campus)
                        .OrderBy(r => r.Nombre)
                        .ToList();
                }

                AplicarFiltroBusqueda();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar restaurantes: {ex.Message}", "OK");
            }
        }

        private void AplicarFiltroBusqueda()
        {
            string term = (SearchRest.Text ?? "").Trim();

            var filtrado = string.IsNullOrEmpty(term)
                ? _restaurantesBase
                : _restaurantesBase.Where(r => r.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

            ListaRestaurantes.ItemsSource = filtrado;
        }

        private void OnCampusChanged(object sender, EventArgs e)
        {
            if (PickerCampus.SelectedItem == null)
                return;

            string campusSeleccionado = PickerCampus.SelectedItem.ToString();

            // Actualiza el chip visual
            LblCampus.Text = campusSeleccionado;

            // ?? VUELVE A CONSULTAR LA BD
            CargarRestaurantes();
        }

        private void OnCampusChipTapped(object sender, TappedEventArgs e)
        {
            // Abre el Picker cuando toques el "chip"
            PickerCampus?.Focus();
        }


        private void OnBuscarChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroBusqueda();
        }

        private async void OnRestauranteSeleccionado(object sender, TappedEventArgs e)
        {
            try
            {
                if (e.Parameter is not Restaurante rest)
                    return;

                await Navigation.PushAsync(new PlatosPage(rest.Id));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error al navegar", ex.Message, "OK");
            }
        }

        private async void OnCarritoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CarritoPage());
        }
    }
}
