using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class PermissoesViewModel : ObservableObject
{
    private readonly UsuarioService        _svc;
    private readonly PermissaoTelaService  _telaSvc;

    [ObservableProperty] private ObservableCollection<Usuario> _usuarios = new();
    [ObservableProperty] private Usuario? _usuarioSelecionado;
    [ObservableProperty] private ObservableCollection<PermissaoOpcao>  _opcoes = new();
    [ObservableProperty] private ObservableCollection<TelaOpcao>       _telas  = new();
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    private CancellationTokenSource? _msgCts;

    public PermissoesViewModel(UsuarioService svc, PermissaoTelaService telaSvc)
    {
        _svc     = svc;
        _telaSvc = telaSvc;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            Usuarios = new ObservableCollection<Usuario>((await _svc.ListarAsync()).Where(u => u.Perfil != "Admin"));
            if (UsuarioSelecionado == null)
                UsuarioSelecionado = Usuarios.FirstOrDefault();
            else
                await CarregarPermissoesDoUsuarioAsync(UsuarioSelecionado);
        }
        finally { IsLoading = false; }
    }

    partial void OnUsuarioSelecionadoChanged(Usuario? value)
    {
        if (value == null) { Opcoes = new(); Telas = new(); return; }
        _ = CarregarPermissoesDoUsuarioAsync(value);
    }

    private async Task CarregarPermissoesDoUsuarioAsync(Usuario usuario)
    {
        // Permissões funcionais
        var concedidas = (usuario.PermissoesCustom ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Opcoes = new ObservableCollection<PermissaoOpcao>(
            Permissoes.Todas.Select(p => new PermissaoOpcao(p.Chave, p.Label, p.Descricao, concedidas.Contains(p.Chave))));

        // Visibilidade de telas
        var mapa = await _telaSvc.ObterMapaAsync(usuario.Id);
        Telas = new ObservableCollection<TelaOpcao>(
            TelasApp.Todas.Select(t =>
            {
                var nivelEfetivo = PermissaoTelaService.NivelEfetivo(usuario, t.Chave, mapa);
                var visivel      = nivelEfetivo != NiveisPermissaoTela.Nenhum;
                return new TelaOpcao(t.Chave, t.Label, visivel);
            }));
    }

    [RelayCommand]
    public void SelecionarUsuario(Usuario usuario) => UsuarioSelecionado = usuario;

    [RelayCommand]
    public void ToggleTela(TelaOpcao tela) => tela.Visivel = !tela.Visivel;

    [RelayCommand]
    public async Task SalvarAsync()
    {
        if (UsuarioSelecionado == null) return;

        // Salva permissões funcionais
        var chaves = Opcoes.Where(o => o.Concedida).Select(o => o.Chave);
        await _svc.AtualizarPermissoesCustomAsync(UsuarioSelecionado.Id, chaves);

        // Salva visibilidade de telas em paralelo
        await Task.WhenAll(Telas.Select(tela =>
        {
            var nivel = tela.Visivel ? NiveisPermissaoTela.Editar : NiveisPermissaoTela.Nenhum;
            return _telaSvc.DefinirAsync(UsuarioSelecionado.Id, tela.Chave, nivel);
        }));

        Mensagem = "Permissões salvas com sucesso.";
        _msgCts?.Cancel();
        var cts = _msgCts = new CancellationTokenSource();
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3000, cts.Token).ContinueWith(_ => Mensagem = null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}

public partial class PermissaoOpcao : ObservableObject
{
    public string Chave     { get; }
    public string Label     { get; }
    public string Descricao { get; }
    [ObservableProperty] private bool _concedida;

    public PermissaoOpcao(string chave, string label, string descricao, bool concedida)
    {
        Chave = chave; Label = label; Descricao = descricao; Concedida = concedida;
    }
}

public partial class TelaOpcao : ObservableObject
{
    public string Chave { get; }
    public string Label { get; }
    [ObservableProperty] private bool _visivel;

    public TelaOpcao(string chave, string label, bool visivel)
    {
        Chave = chave; Label = label; Visivel = visivel;
    }
}
