using Microsoft.Extensions.Logging;
using Reverie.Services;

namespace Reverie
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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Register JournalService as singleton
            builder.Services.AddSingleton<JournalService>();

            builder.Services.AddSingleton<Reverie.Services.LockService>();

            builder.Services.AddSingleton<Reverie.Services.DashboardService>();

            builder.Services.AddScoped<ThemeService>();


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}