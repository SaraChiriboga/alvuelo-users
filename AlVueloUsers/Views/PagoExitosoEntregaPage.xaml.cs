namespace AlVueloUsers.Views
{
    public partial class PagoExitosoPage : ContentPage
    {
        public PagoExitosoPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 1. Dejar que la animación Lottie corra sola un momento (1.5 seg)
            await Task.Delay(1500);

            // 2. Animar la entrada de los textos (FadeIn + SlideUp)
            await Task.WhenAll(
                AnimarEntrada(LabelTitulo, 0),
                AnimarEntrada(LabelSubtitulo, 200),
                AnimarEntrada(BotonInicio, 400)
            );
        }

        private async Task AnimarEntrada(VisualElement elemento, int delay)
        {
            await Task.Delay(delay);
            elemento.TranslationY = 30; // Empieza abajo

            await Task.WhenAll(
                elemento.FadeTo(1, 600, Easing.CubicOut),       // Aparece
                elemento.TranslateTo(0, 0, 600, Easing.CubicOut) // Sube
            );
        }

        private void OnEntendidoClicked(object sender, EventArgs e)
        {
            // RESETEAR LA NAVEGACIÓN
            // Vuelve al HomePage y borra el historial para que no puedan volver al pago
            Application.Current.MainPage = new NavigationPage(new PagoPage());
            // Si tu página principal se llama de otra forma, cambia 'HomePage' por el nombre correcto.
        }
    }
}