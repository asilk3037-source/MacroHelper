using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Mobile.Services;

namespace MacroHelper.Mobile.ViewModels;

public partial class ConfigPageViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private string _nomeUsuario   = Preferences.Get("user_nome",  "Usuário");
    [ObservableProperty] private string _email         = Preferences.Get("user_email", string.Empty);
    [ObservableProperty] private string _apiUrl        = Preferences.Get("api_url",    "http://localhost:5000");
    [ObservableProperty] private string _statusConexao = string.Empty;
    [ObservableProperty] private string _statusCor     = "#8886a8";

    public ConfigPageViewModel(ApiService api) => _api = api;

    [RelayCommand]
    public async Task SalvarUrlAsync()
    {
        _api.ConfigurarUrl(ApiUrl);
        StatusConexao = "Testando..."; StatusCor = "#8886a8";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var res = await http.GetAsync($"{ApiUrl.TrimEnd('/')}/swagger/v1/swagger.json");
            StatusConexao = res.IsSuccessStatusCode ? "Conectado." : "API não encontrada.";
            StatusCor = res.IsSuccessStatusCode ? "#4ade80" : "#f87171";
        }
        catch { StatusConexao = "Sem conexão."; StatusCor = "#f87171"; }
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        _api.Logout();
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand] public void TemaClaro() => App.Current!.UserAppTheme = AppTheme.Light;
    [RelayCommand] public void TemaEscuro() => App.Current!.UserAppTheme = AppTheme.Dark;
}
