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

public partial class PermissaoTelaItem : ObservableObject
{
    public string Chave { get; }
    public string Label { get; }
    [ObservableProperty] private string _nivel;

    public PermissaoTelaItem(string chave, string label, string nivel)
    {
        Chave = chave; Label = label; _nivel = nivel;
    }
}

public partial class UsuariosViewModel : ObservableObject
{
    private readonly UsuarioService       _svc;
    private readonly CategoriaService     _catService;
    private readonly GrupoService         _grupoService;
    private readonly PermissaoTelaService _permissaoTelaService;

    public string[] NiveisDisponiveis => NiveisPermissaoTela.Ordem;

    [ObservableProperty] private bool _podeEditarTela  = true;
    [ObservableProperty] private bool _podeExcluirTela = true;

    [ObservableProperty] private ObservableCollection<Usuario> _usuarios = new();
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;
    [ObservableProperty] private int?    _confirmandoExclusaoId;

    // ── Painel de permissões (categorias + grupo + telas) ──
    [ObservableProperty] private bool     _mostrarPermissoes = false;
    [ObservableProperty] private Usuario? _usuarioPermissoes;
    [ObservableProperty] private ObservableCollection<CategoriaCheckItem> _categoriasPermissao = new();
    [ObservableProperty] private ObservableCollection<Grupo> _gruposDisponiveis = new();
    [ObservableProperty] private int?     _grupoSelecionadoId;
    [ObservableProperty] private string   _novoGrupoNome = string.Empty;
    [ObservableProperty] private ObservableCollection<PermissaoTelaItem> _permissoesTela = new();

    // ── Formulário de criação/edição de usuário ──
    [ObservableProperty] private bool     _mostrarFormUsuario = false;
    [ObservableProperty] private Usuario? _usuarioEditando;
    [ObservableProperty] private string   _formNome = string.Empty;
    [ObservableProperty] private string   _formEmail = string.Empty;
    [ObservableProperty] private string   _formSenha = string.Empty;
    [ObservableProperty] private string   _formPerfil = "Usuario";
    [ObservableProperty] private bool     _formAtivo = true;
    [ObservableProperty] private string?  _formErro;
    [ObservableProperty] private bool     _salvandoUsuario = false;

    public UsuariosViewModel(UsuarioService svc, CategoriaService catService, GrupoService grupoService,
        PermissaoTelaService permissaoTelaService)
    {
        _svc                  = svc;
        _catService           = catService;
        _grupoService         = grupoService;
        _permissaoTelaService = permissaoTelaService;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            await AtualizarNivelTelaAsync();
            Usuarios = new ObservableCollection<Usuario>(await _svc.ListarAsync());
        }
        finally { IsLoading = false; }
    }

    private async Task AtualizarNivelTelaAsync()
    {
        var usuario = _svc.UsuarioAtual;
        if (usuario == null) return;
        var nivel = await _permissaoTelaService.ObterNivelEfetivoAsync(usuario, "usuarios");
        PodeEditarTela  = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Editar);
        PodeExcluirTela = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Excluir);
    }

    // ── Criar / editar usuário ──

    [RelayCommand]
    public void NovoUsuario()
    {
        UsuarioEditando = null;
        FormNome = string.Empty; FormEmail = string.Empty; FormSenha = string.Empty;
        FormPerfil = "Usuario"; FormAtivo = true; FormErro = null;
        MostrarFormUsuario = true;
    }

    [RelayCommand]
    public void EditarUsuario(Usuario usuario)
    {
        UsuarioEditando = usuario;
        FormNome = usuario.Nome; FormEmail = usuario.Email; FormSenha = string.Empty;
        FormPerfil = usuario.Perfil; FormAtivo = usuario.Ativo; FormErro = null;
        MostrarFormUsuario = true;
    }

    [RelayCommand]
    public void CancelarFormUsuario() => MostrarFormUsuario = false;

    [RelayCommand]
    public async Task SalvarUsuario()
    {
        FormErro = null;
        SalvandoUsuario = true;
        try
        {
            var perfil = FormPerfil;
            if (UsuarioEditando == null)
            {
                var (ok, msg) = await _svc.CriarComoAdminAsync(FormNome, FormEmail, FormSenha, perfil,
                    (acc, refresh) => SupabaseSessionStore.Save(acc, refresh));
                if (!ok) { FormErro = msg; return; }
            }
            else
            {
                var (ok, msg) = await _svc.AtualizarComoAdminAsync(UsuarioEditando.Id, FormNome, FormEmail, perfil, FormAtivo);
                if (!ok) { FormErro = msg; return; }
            }

            MostrarFormUsuario = false;
            MostrarMsg(UsuarioEditando == null ? "Usuário criado com sucesso!" : "Usuário atualizado!", true);
            await CarregarAsync();
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex) when (ex.Message.Contains("rate_limit"))
        {
            FormErro = "Limite de envio de e-mails do Supabase excedido. Aguarde alguns minutos e tente novamente.";
        }
        catch (Exception ex)
        {
            FormErro = $"Erro ao salvar usuário: {ex.Message}";
        }
        finally { SalvandoUsuario = false; }
    }

    // ── Excluir usuário (exige um segundo clique de confirmação no mesmo item) ──

    [RelayCommand]
    public async Task ExcluirUsuario(Usuario usuario)
    {
        if (ConfirmandoExclusaoId != usuario.Id) { ConfirmandoExclusaoId = usuario.Id; return; }

        ConfirmandoExclusaoId = null;
        try
        {
            var (ok, msg) = await _svc.ExcluirAsync(usuario.Id);
            MostrarMsg(msg, ok);
            if (ok) await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarMsg($"Erro ao excluir usuário: {ex.Message}", false);
        }
    }

    [RelayCommand]
    public void CancelarExclusao() => ConfirmandoExclusaoId = null;

    // ── Grupos (inalterado) ──

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

    // ── Painel de permissões (categorias + grupo + telas) ──

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

        var mapaTelas = usuario.Perfil == "Admin"
            ? new Dictionary<string, string>()
            : await _permissaoTelaService.ObterMapaAsync(usuario.Id);
        PermissoesTela = new ObservableCollection<PermissaoTelaItem>(
            TelasApp.Todas.Select(t => new PermissaoTelaItem(t.Chave, t.Label,
                mapaTelas.TryGetValue(t.Chave, out var nivel) ? nivel : PermissaoTelaService.NivelEfetivo(usuario, t.Chave, null))));

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

        if (UsuarioPermissoes.Perfil != "Admin")
            foreach (var item in PermissoesTela)
                await _permissaoTelaService.DefinirAsync(UsuarioPermissoes.Id, item.Chave, item.Nivel);

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
