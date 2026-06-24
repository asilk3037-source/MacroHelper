using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class CategoriaCheckItem : ObservableObject
{
    public int    Id   { get; }
    public string Nome { get; }
    [ObservableProperty] private bool _selecionada;

    public CategoriaCheckItem(int id, string nome, bool selecionada)
    {
        Id = id; Nome = nome; Selecionada = selecionada;
    }
}

public partial class UsuariosViewModel : ObservableObject
{
    private readonly UsuarioService    _svc;
    private readonly CategoriaService  _catService;
    private readonly GrupoService      _grupoService;

    [ObservableProperty] private ObservableCollection<Usuario> _usuarios = new();
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;

    [ObservableProperty] private bool     _mostrarPermissoes = false;
    [ObservableProperty] private Usuario? _usuarioPermissoes;
    [ObservableProperty] private ObservableCollection<CategoriaCheckItem> _categoriasPermissao = new();
    [ObservableProperty] private ObservableCollection<Grupo> _gruposDisponiveis = new();
    [ObservableProperty] private int?     _grupoSelecionadoId;
    [ObservableProperty] private string   _novoGrupoNome = string.Empty;

    public UsuariosViewModel(UsuarioService svc, CategoriaService catService, GrupoService grupoService)
    {
        _svc          = svc;
        _catService   = catService;
        _grupoService = grupoService;
    }

    [RelayCommand]
    public async Task AdicionarGrupo()
    {
        var (ok, msg) = await _grupoService.CriarAsync(NovoGrupoNome);
        if (!ok) { MostrarMsg(msg, false); return; }
        NovoGrupoNome = string.Empty;
        GruposDisponiveis = new ObservableCollection<Grupo>(await _grupoService.ObterTodosAsync());
    }

    [RelayCommand]
    public async Task ExcluirGrupo(Grupo grupo)
    {
        await _grupoService.ExcluirAsync(grupo.Id);
        GruposDisponiveis = new ObservableCollection<Grupo>(await _grupoService.ObterTodosAsync());
        if (GrupoSelecionadoId == grupo.Id) GrupoSelecionadoId = null;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try { Usuarios = new ObservableCollection<Usuario>(await _svc.ListarAsync()); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task AbrirPermissoes(Usuario usuario)
    {
        UsuarioPermissoes = usuario;
        var permitidas = (usuario.CategoriasPermitidas ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse).ToHashSet();

        var categorias = await _catService.ObterTodosAsync();
        CategoriasPermissao = new ObservableCollection<CategoriaCheckItem>(
            categorias.Select(c => new CategoriaCheckItem(c.Id, c.Nome, permitidas.Contains(c.Id))));

        GruposDisponiveis  = new ObservableCollection<Grupo>(await _grupoService.ObterTodosAsync());
        GrupoSelecionadoId = usuario.GrupoId;

        MostrarPermissoes = true;
    }

    [RelayCommand]
    public void FecharPermissoes() => MostrarPermissoes = false;

    [RelayCommand]
    public async Task SalvarPermissoes()
    {
        if (UsuarioPermissoes == null) return;
        var idsSelecionados = CategoriasPermissao.Where(c => c.Selecionada).Select(c => c.Id);
        await _svc.AtualizarPermissoesAsync(UsuarioPermissoes.Id, idsSelecionados);
        await _grupoService.AtribuirUsuarioAsync(UsuarioPermissoes.Id, GrupoSelecionadoId);
        MostrarPermissoes = false;
        MostrarMsg("Permissões atualizadas!", true);
        await CarregarAsync();
    }

    private void MostrarMsg(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => Mensagem = null,
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}
