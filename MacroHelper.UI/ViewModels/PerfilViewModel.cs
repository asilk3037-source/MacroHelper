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

    private CancellationTokenSource? _perfilCts;
    private CancellationTokenSource? _senhaCts;

    public PerfilViewModel(UsuarioService svc)
    {
        _svc = svc;
        var u = svc.UsuarioAtual;
        Nome   = u?.Nome   ?? string.Empty;
        Email  = u?.Email  ?? string.Empty;
        Perfil = u?.Perfil == "Admin" ? "Administrador" : "Usuário";
    }

    public string Iniciais => string.Join("",
        Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(s => char.ToUpper(s[0]).ToString()));

    public string DataCriacaoFormatada =>
        _svc.UsuarioAtual?.DataCriacao is DateTime d ? $"desde {d:dd/MM/yyyy}" : "";

    public double SenhaForcaPercent
    {
        get
        {
            if (NovaSenha.Length == 0) return 0;
            if (NovaSenha.Length < 6) return 25;
            if (NovaSenha.Length < 8) return 50;
            return NovaSenha.Any(char.IsDigit) && NovaSenha.Any(char.IsLetter) ? 100 : 75;
        }
    }

    public string SenhaForcaLabel => SenhaForcaPercent switch
    {
        0    => "",
        25   => "Muito fraca",
        50   => "Fraca",
        75   => "Boa",
        _    => "Forte"
    };

    partial void OnNomeChanged(string value)
    {
        OnPropertyChanged(nameof(Iniciais));
    }

    partial void OnNovaSenhaChanged(string value)
    {
        OnPropertyChanged(nameof(SenhaForcaPercent));
        OnPropertyChanged(nameof(SenhaForcaLabel));
    }

    [RelayCommand]
    public async Task SalvarPerfilAsync()
    {
        var usuarioId = _svc.UsuarioAtual?.Id ?? 0;
        var (ok, msg) = await _svc.AtualizarPerfilAsync(usuarioId, Nome, Email);
        MensagemPerfil = msg; MensagemPerfilSucesso = ok;
        _perfilCts?.Cancel();
        var pcts = _perfilCts = new CancellationTokenSource();
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500, pcts.Token).ContinueWith(_ => MensagemPerfil = null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.FromCurrentSynchronizationContext());
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
        _senhaCts?.Cancel();
        var scts = _senhaCts = new CancellationTokenSource();
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500, scts.Token).ContinueWith(_ => MensagemSenha = null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}
