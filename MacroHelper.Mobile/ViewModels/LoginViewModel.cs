using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Mobile.Services;

namespace MacroHelper.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private string _email     = string.Empty;
    [ObservableProperty] private string _senha     = string.Empty;
    [ObservableProperty] private string _erro      = string.Empty;
    [ObservableProperty] private bool   _carregando = false;
    [ObservableProperty] private bool   _mostrarCriar = false;
    [ObservableProperty] private string _novoNome   = string.Empty;
    [ObservableProperty] private string _novoEmail  = string.Empty;
    [ObservableProperty] private string _novaSenha  = string.Empty;
    [ObservableProperty] private string _confirmar  = string.Empty;
    [ObservableProperty] private string _apiUrl     = Preferences.Get("api_url", "http://localhost:5000");

    public event Action? LoginSucesso;

    public LoginViewModel(ApiService api) => _api = api;

    [RelayCommand]
    public async Task EntrarAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
        { Erro = "Preencha e-mail e senha."; return; }

        Erro = string.Empty; Carregando = true;
        _api.ConfigurarUrl(ApiUrl);
        try
        {
            var (ok, msg, _) = await _api.LoginAsync(Email, Senha);
            if (!ok) { Erro = msg; return; }
            LoginSucesso?.Invoke();
        }
        finally { Carregando = false; }
    }

    [RelayCommand]
    public async Task CriarContaAsync()
    {
        Erro = string.Empty; Carregando = true;
        _api.ConfigurarUrl(ApiUrl);
        try
        {
            var (ok, msg) = await _api.CriarContaAsync(NovoNome, NovoEmail, NovaSenha, Confirmar);
            if (!ok) { Erro = msg; return; }
            LoginSucesso?.Invoke();
        }
        finally { Carregando = false; }
    }

    [RelayCommand] public void AlternarCriar() => MostrarCriar = !MostrarCriar;
}
