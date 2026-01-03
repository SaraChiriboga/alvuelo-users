using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers; // Necesario para los Handlers

namespace AlVueloUsers
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Inter-Italic-Variable.ttf", "InterItalic");
                    fonts.AddFont("Inter-Variable.ttf", "Inter");
                    fonts.AddFont("Font Awesome 7 Brands-Regular-400.otf", "FABrands");
                    fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FARegular");
                    fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FASolid");
                });

            // LOGICA PARA QUITAR LA BARRA MOLESTA EN ANDROID
#if ANDROID
            PickerHandler.Mapper.AppendToMapping("NoLineAndroid", (handler, view) =>
            {
                // Eliminamos el fondo (background) que contiene la línea
                handler.PlatformView.Background = null;
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                
                // Opcional: Quitar padding interno para que quede bien alineado
                handler.PlatformView.SetPadding(0, 0, 0, 0);
            });
#endif

            return builder.Build();
        }
    }
}