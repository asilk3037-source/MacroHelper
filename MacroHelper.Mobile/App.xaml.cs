using MacroHelper.Mobile.Pages;
using MacroHelper.Mobile.Services;

namespace MacroHelper.Mobile;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        var api = services.GetRequiredService<ApiService>();

        if (api.Autenticado)
            MainPage = new AppShell();
        else
            MainPage = services.GetRequiredService<LoginPage>();
    }
}
