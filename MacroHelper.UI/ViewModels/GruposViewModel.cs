using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class GruposViewModel : ObservableObject
{
    private readonly GrupoService   _svc;
    private readonly UsuarioService _usuarioService;
    private readonly PermissaoTelaService? _permissaoTelaService;

    [ObservableProperty] private bool _podeEditarTela  = true;
    [ObservableProperty] private bool _podeExcluirTela = true;

    [ObservableProperty] private ObservableCollection<Grupo> _grupos = new();
    [ObservableProperty] private ObservableCollection<Usuario> _todosUsuarios = new();
    [ObservableProperty] private bool    _mostrarFormulario = false;
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;

    [ObservableProperty] private int     _formId = 0;
    [ObservableProperty] private string  _formNome = string.Empty;
    [ObservableProperty] private string  _formTitulo = "Novo Grupo";
    [ObservableProperty] private string? _formErro;

    public GruposViewModel(GrupoService svc, UsuarioService usuarioService, PermissaoTelaService? permissaoTelaService = null)
    {
        _svc = svc;
        _usuarioService = usuarioService;
        _permissaoTelaService = permissaoTelaService;
    }

    private async Task AtualizarNivelTelaAsync()
    {
        var usuario = _usuarioService.UsuarioAtual;
        if (usuario == null || _permissaoTelaService == null) return;
        var nivel = await _permissaoTelaService.ObterNivelEfetivoAsync(usuario, "grupos");
        PodeEditarTela  = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Editar);
        PodeExcluirTela = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Excluir);
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            await AtualizarNivelTelaAsync();
            TodosUsuarios = new ObservableCollection<Usuario>(await _usuarioService.ListarAsync());
            Grupos = new ObservableCollection<Grupo>(await _svc.ObterTodosAsync());
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public void NovoGrupo()
    {
        FormId = 0; FormNome = string.Empty; FormErro = null; FormTitulo = "Novo Grupo"; MostrarFormulario = true;
    }

    [RelayCommand]
    public void EditarGrupo(Grupo g)
    {
        FormId = g.Id; FormNome = g.Nome; FormErro = null; FormTitulo = "Editar Grupo"; MostrarFormulario = true;
    }

    [RelayCommand]
    public void CancelarForm() => MostrarFormulario = false;

    [RelayCommand]
    public async Task SalvarGrupoAsync()
    {
        FormErro = null;
        var (ok, msg) = FormId == 0
            ? await _svc.CriarAsync(FormNome)
            : await _svc.EditarAsync(FormId, FormNome);
        if (!ok) { FormErro = msg; return; }
        MostrarFormulario = false;
        await CarregarAsync();
        MostrarMensagem(msg, true);
    }

    [RelayCommand]
    public async Task ExcluirGrupoAsync(Grupo g)
    {
        await _svc.ExcluirAsync(g.Id);
        await CarregarAsync();
        MostrarMensagem("Grupo excluído.", true);
    }

    [RelayCommand]
    public async Task AlterarGrupoUsuarioAsync(Usuario usuario)
    {
        await _svc.AtribuirUsuarioAsync(usuario.Id, usuario.GrupoId);
        await CarregarAsync();
        MostrarMensagem("Membro atualizado.", true);
    }

    private void MostrarMensagem(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => Mensagem = null, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
