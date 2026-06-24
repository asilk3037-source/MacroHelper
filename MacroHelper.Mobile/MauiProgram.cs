using CommunityToolkit.Maui;
using MacroHelper.Mobile.Pages;
using MacroHelper.Mobile.Services;
using MacroHelper.Mobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace MacroHelper.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<ApiService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MacrosViewModel>();
        builder.Services.AddTransient<MacroFormViewModel>();
        builder.Services.AddTransient<CategoriasPageViewModel>();
        builder.Services.AddTransient<RelatorioPageViewModel>();
        builder.Services.AddTransient<ConfigPageViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MacrosPage>();
        builder.Services.AddTransient<MacroFormPage>();
        builder.Services.AddTransient<CategoriasPage>();
        builder.Services.AddTransient<RelatorioPage>();
        builder.Services.AddTransient<ConfigPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
