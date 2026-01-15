using AlVueloUsers.Data;
using AlVueloUsers.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using SkiaSharp.Views.Maui.Controls.Hosting; // Necesario para los Handlers

namespace AlVueloUsers
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Inter-Italic-Variable.ttf", "InterItalic");
                    fonts.AddFont("Inter-Variable.ttf", "Inter");
                    fonts.AddFont("Font Awesome 7 Brands-Regular-400.otf", "FABrands");
                    fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FARegular");
                    fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FASolid");
                });

            // --- REGISTRO DE SERVICIOS (DI) ---
            builder.Services.AddDbContext<AlVueloDbContext>();

            // Registramos las páginas para que puedan recibir el DbContext
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegistroPage>();
            // LOGICA PARA QUITAR LA BARRA MOLESTA EN ANDROID
#if ANDROID
            // 1. ELIMINAR LÍNEA DE LOS ENTRYS (TODO EL PROGRAMA)
            EntryHandler.Mapper.AppendToMapping("NoLineEntry", (handler, view) =>
            {
                // Quitamos el underline de Android
                handler.PlatformView.Background = null;
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.SetPadding(0, 0, 0, 0);
            });

            // 2. ELIMINAR LÍNEA DE LOS PICKERS
            PickerHandler.Mapper.AppendToMapping("NoLinePicker", (handler, view) =>
            {
                handler.PlatformView.Background = null;
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            });

            // Cambiar color del indicador (botón) del RadioButton en Android al color AlVueloRed
            RadioButtonHandler.Mapper.AppendToMapping("AlVueloRadioColor", (handler, view) =>
            {
                try
                {
                    // Color hex definido en recursos XAML: #FF5757
                    var androidColor = Android.Graphics.Color.ParseColor("#000000");
                    var stateList = Android.Content.Res.ColorStateList.ValueOf(androidColor);

                    // handler.PlatformView is a Android.Views.View at compile time; cast to native types safely
                    var nativeView = handler.PlatformView;

                    if (nativeView is Android.Widget.CompoundButton compoundButton)
                    {
                        compoundButton.ButtonTintList = stateList;
                    }

                    if (nativeView is Android.Widget.TextView textView)
                    {
                        textView.SetTextColor(androidColor);
                    }
                }
                catch
                {
                    // Silenciar errores para evitar romper el build en entornos no Android
                }
            });
#endif

            return builder.Build();
        }
    }
}