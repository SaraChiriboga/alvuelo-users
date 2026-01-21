using AlVueloUsers.Services;
using AlVueloUsers.Models;
using System.Collections.Specialized;

namespace AlVueloUsers.Views
{
    public partial class CarritoPage : ContentPage
    {
        public CarritoPage()
        {
            InitializeComponent();

            // Una sola vez
            ListaCarrito.ItemsSource = CarritoService.Instancia.Items;

            // Para refrescar cuando se agregan o eliminan items
            CarritoService.Instancia.Items.CollectionChanged += Items_CollectionChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RefrescarUI();
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Se dispara cuando agregas o quitas productos del carrito
            MainThread.BeginInvokeOnMainThread(RefrescarUI);
        }

        private void RefrescarUI()
        {
            var carrito = CarritoService.Instancia;

            // Totales
            LblSubtotal.Text = carrito.Subtotal.ToString("C2");
            LblServicio.Text = carrito.Servicio.ToString("C2");
            LblTotal.Text = carrito.Total.ToString("C2");

            // Cantidad de items (líneas en carrito)
            var lineas = carrito.Items.Count;
            LblCantidadItems.Text = $"{lineas} item{(lineas == 1 ? "" : "s")}";

            // Vacío / lista visible
            bool vacio = lineas == 0;
            EmptyCartMessage.IsVisible = vacio;
            ListaCarrito.IsVisible = !vacio;
        }

        private void OnMasTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is CarritoItem item)
            {
                CarritoService.Instancia.Incrementar(item);

                // Esto es necesario porque Incrementar no dispara CollectionChanged
                RefrescarUI();

                // Truco para refrescar el item visualmente si no actualiza el Subtotal en pantalla:
                ListaCarrito.ItemsSource = null;
                ListaCarrito.ItemsSource = CarritoService.Instancia.Items;
            }
        }

        private void OnMenosTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is CarritoItem item)
            {
                CarritoService.Instancia.Decrementar(item);

                RefrescarUI();

                ListaCarrito.ItemsSource = null;
                ListaCarrito.ItemsSource = CarritoService.Instancia.Items;
            }
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnHomeTapped(object sender, EventArgs e)
        {
            await Navigation.PopToRootAsync();
        }

        private async void OnContinuar(object sender, EventArgs e)
        {
            if (CarritoService.Instancia.Items.Count == 0)
            {
                await DisplayAlert("Carrito", "Tu carrito está vacío.", "OK");
                return;
            }

            await Navigation.PushAsync(new PagoPage());
        }

    }
}
