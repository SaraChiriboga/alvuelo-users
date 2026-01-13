namespace AlVueloUsers.Views;

public partial class PagoExitosoConsumoPage : ContentPage
{
    // Constructor que recibe el PIN
    public PagoExitosoConsumoPage(string pin)
    {
        InitializeComponent();

        // Asignar el PIN recibido a la etiqueta
        LabelPin.Text = pin;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Secuencia de animación de entrada (Fade In y Slide Up sutil)
        await Task.WhenAll(
            LabelTitulo.FadeTo(1, 600, Easing.CubicOut),
            LabelTitulo.TranslateTo(0, 0, 600, Easing.CubicOut)
        );

        await Task.WhenAll(
            LabelSubtitulo.FadeTo(1, 600, Easing.CubicOut),
            LabelSubtitulo.TranslateTo(0, 0, 600, Easing.CubicOut)
        );

        // Animación especial para el PIN (Scale Up)
        await BorderPin.ScaleTo(1.1, 200);
        await BorderPin.FadeTo(1, 400);
        await BorderPin.ScaleTo(1.0, 200, Easing.SpringOut);

        await Task.WhenAll(
            BotonInicio.FadeTo(1, 600, Easing.CubicOut),
            BotonInicio.TranslateTo(0, 0, 600, Easing.CubicOut)
        );
    }

    private async void OnEntendidoClicked(object sender, EventArgs e)
    {
        // 1. Animación del botón (Feedback visual)
        await BotonInicio.ScaleTo(0.95, 100);
        await BotonInicio.ScaleTo(1.0, 100);

        // 2. TRUCO DE NAVEGACIÓN: Limpiar el historial "debajo" del modal
        // Obtenemos la navegación principal de la app
        if (Application.Current.MainPage is NavigationPage navPage)
        {
            // Buscamos si la página de selección de mesa está en la pila
            var paginaIntermedia = navPage.Navigation.NavigationStack
                                   .FirstOrDefault(p => p is SeleccionMesaPage);

            // Si existe, la eliminamos antes de cerrar el modal
            if (paginaIntermedia != null)
            {
                navPage.Navigation.RemovePage(paginaIntermedia);
            }
        }

        // 3. CERRAR EL MODAL
        // Al cerrarse, mostrará la última página que quedó en la pila (PagoPage)
        await Navigation.PopModalAsync();
    }
}