using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Services;

namespace MacroHelper.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly UsuarioService _usuarioService;

    [ObservableProperty] private string  _email     = string.Empty;
    [ObservableProperty] private string  _erro      = string.Empty;
    [ObservableProperty] private bool    _carregando = false;
    [ObservableProperty] private bool    _mostrarCriar = false;

    // Campos de criar conta
    [ObservableProperty] private string _novoNome   = string.Empty;
    [ObservableProperty] private string _novoEmail  = string.Empty;
    [ObservableProperty] private string _erroCriar  = string.Empty;
    [ObservableProperty] private bool   _criandoConta = false;

    public event Action? LoginSucesso;
    public event Action? ContaCriada;

    // Senha é lida via code-behind (PasswordBox não faz bind por segurança)
    public string SenhaTemp     { get; set; } = string.Empty;
    public string NovaSenhaTemp { get; set; } = string.Empty;
    public string ConfirmarTemp { get; set; } = string.Empty;

    public LoginViewModel(UsuarioService usuarioService)
        => _usuarioService = usuarioService;

    [RelayCommand]
    public async Task EntrarAsync()
    {
        Erro = string.Empty;
        Carregando = true;
        try
        {
            var (ok, msg, _) = await _usuarioService.LoginAsync(Email, SenhaTemp);
            if (!ok) { Erro = msg; return; }
            LoginSucesso?.Invoke();
        }
        finally { Carregando = false; }
    }

    [RelayCommand]
    public async Task EntrarComGoogleAsync() => await EntrarComProvedorAsync("Google");

    [RelayCommand]
    public async Task EntrarComMicrosoftAsync() => await EntrarComProvedorAsync("Microsoft");

    private async Task EntrarComProvedorAsync(string provedor)
    {
        Erro = string.Empty;
        Carregando = true;
        try
        {
            var (ok, msg, _) = await _usuarioService.LoginComProvedorAsync(provedor);
            if (!ok) { Erro = msg; return; }
            LoginSucesso?.Invoke();
        }
        finally { Carregando = false; }
    }

    [RelayCommand]
    public void AbrirCriarConta()
    {
        MostrarCriar = true;
        ErroCriar    = string.Empty;
    }

    [RelayCommand]
    public void FecharCriarConta() => MostrarCriar = false;

    [RelayCommand]
    public async Task CriarContaAsync()
    {
        ErroCriar = string.Empty;
        CriandoConta = true;
        try
        {
            var (ok, msg) = await _usuarioService.CriarAsync(
                NovoNome, NovoEmail, NovaSenhaTemp, ConfirmarTemp);
            if (!ok) { ErroCriar = msg; return; }
            MostrarCriar = false;
            Erro = string.Empty;
            // Auto-login
            await _usuarioService.LoginAsync(NovoEmail, NovaSenhaTemp);
            LoginSucesso?.Invoke();
        }
        finally { CriandoConta = false; }
    }
}
