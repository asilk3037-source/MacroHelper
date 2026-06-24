using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class PermissoesViewModel : ObservableObject
{
    private readonly UsuarioService _svc;

    [ObservableProperty] private ObservableCollection<Usuario> _usuarios = new();
    [ObservableProperty] private Usuario? _usuarioSelecionado;
    [ObservableProperty] private ObservableCollection<PermissaoOpcao> _opcoes = new();
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;

    public PermissoesViewModel(UsuarioService svc) => _svc = svc;

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            Usuarios = new ObservableCollection<Usuario>((await _svc.ListarAsync()).Where(u => u.Perfil != "Admin"));
            UsuarioSelecionado = Usuarios.FirstOrDefault();
        }
        finally { IsLoading = false; }
    }

    partial void OnUsuarioSelecionadoChanged(Usuario? value)
    {
        var concedidas = (value?.PermissoesCustom ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Opcoes = new ObservableCollection<PermissaoOpcao>(
            Permissoes.Todas.Select(p => new PermissaoOpcao(p.Chave, p.Label, p.Descricao, concedidas.Contains(p.Chave))));
    }

    [RelayCommand]
    public void SelecionarUsuario(Usuario usuario) => UsuarioSelecionado = usuario;

    [RelayCommand]
    public async Task SalvarAsync()
    {
        if (UsuarioSelecionado == null) return;
        var chaves = Opcoes.Where(o => o.Concedida).Select(o => o.Chave);
        await _svc.AtualizarPermissoesCustomAsync(UsuarioSelecionado.Id, chaves);
        Mensagem = "Permissões atualizadas.";
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3000).ContinueWith(_ => Mensagem = null, TaskScheduler.FromCurrentSynchronizationContext());
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
