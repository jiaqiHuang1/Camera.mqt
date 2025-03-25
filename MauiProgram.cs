using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Camera.Datebase;
using Camera.ViewModel;
using System.Reflection;
using Camera.View;

namespace Camera
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } // Adding a Static Services Attribute
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitMediaElement()
               // .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register CamerasViewModel as a singleton.
            builder.Services.AddSingleton<CamerasViewModel>();

            // Register MenuViewModel as a singleton
            builder.Services.AddSingleton<MenuViewModel>();

            // Register CameraDashboardViewModel as a singleton
            builder.Services.AddSingleton<CameraDashboardViewModel>();

            // Register TwoArcSlider as a single case
            builder.Services.AddSingleton<TwoArcSlider>();

            // Sign up for the Cameralist page
            builder.Services.AddTransient<Cameralist>();

            // Sign up for the MainPage page
            builder.Services.AddTransient<StreamPage>();

            // Sign up for the CameraDashboard page
            builder.Services.AddTransient<CameraDashboard>();



#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Build the app first
            var app = builder.Build();

            // Then assign Services
            Services = app.Services;

            return app;
        }
    }
}
