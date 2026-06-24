using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Services;

namespace MacroHelper.UI.ViewModels;

public partial class PerfilViewModel : ObservableObject
{
    private readonly UsuarioService _svc;

    [ObservableProperty] private string  _nome  = string.Empty;
    [ObservableProperty] private string  _email = string.Empty;
    [ObservableProperty] private string  _perfil = string.Empty;
    [ObservableProperty] private string  _senhaAtual = string.Empty;
    [ObservableProperty] private string  _novaSenha = string.Empty;
    [ObservableProperty] private string  _confirmarSenha = string.Empty;
    [ObservableProperty] private string? _mensagemPerfil;
    [ObservableProperty] private bool    _mensagemPerfilSucesso = true;
    [ObservableProperty] private string? _mensagemSenha;
    [ObservableProperty] private bool    _mensagemSenhaSucesso = true;

    public PerfilViewModel(UsuarioService svc)
    {
        _svc = svc;
        var u = svc.UsuarioAtual;
        Nome   = u?.Nome   ?? string.Empty;
        Email  = u?.Email  ?? string.Empty;
        Perfil = u?.Perfil == "Admin" ? "Administrador" : "Usuário";
    }

    [RelayCommand]
    public async Task SalvarPerfilAsync()
    {
        var usuarioId = _svc.UsuarioAtual?.Id ?? 0;
        var (ok, msg) = await _svc.AtualizarPerfilAsync(usuarioId, Nome, Email);
        MensagemPerfil = msg; MensagemPerfilSucesso = ok;
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => MensagemPerfil = null, TaskScheduler.FromCurrentSynchronizationContext());
    }

    [RelayCommand]
    public async Task TrocarSenhaAsync()
    {
        if (NovaSenha != ConfirmarSenha)
        {
            MensagemSenha = "As senhas não coincidem."; MensagemSenhaSucesso = false;
            return;
        }
        var usuarioId = _svc.UsuarioAtual?.Id ?? 0;
        var (ok, msg) = await _svc.TrocarSenhaAsync(usuarioId, SenhaAtual, NovaSenha);
        MensagemSenha = msg; MensagemSenhaSucesso = ok;
        if (ok) { SenhaAtual = string.Empty; NovaSenha = string.Empty; ConfirmarSenha = string.Empty; }
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => MensagemSenha = null, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
