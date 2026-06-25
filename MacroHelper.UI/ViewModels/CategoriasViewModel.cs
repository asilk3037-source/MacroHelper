using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class CategoriasViewModel : ObservableObject
{
    private readonly CategoriaService _svc;
    private readonly UsuarioService? _usuarioService;
    private readonly PermissaoTelaService? _permissaoTelaService;

    [ObservableProperty] private bool _podeEditarTela  = true;
    [ObservableProperty] private bool _podeExcluirTela = true;

    [ObservableProperty] private ObservableCollection<Categoria> _categorias = new();
    [ObservableProperty] private ObservableCollection<Categoria> _categoriasRaiz = new();
    [ObservableProperty] private bool    _mostrarFormulario = false;
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;

    // Form
    [ObservableProperty] private int     _formId    = 0;
    [ObservableProperty] private string  _formNome  = string.Empty;
    [ObservableProperty] private string  _formIcone = "";

    // Paleta fixa de ícones reais (Segoe MDL2) para categorias — sem emoji livre.
    public List<string> IconesDisponiveis { get; } =
        ["", "", "", "", "", "", "", "", ""];
    [ObservableProperty] private string  _formCor   = "#6C5CE7";
    [ObservableProperty] private int?    _formPaiId = null;
    [ObservableProperty] private string  _formTitulo = "Nova Categoria";
    [ObservableProperty] private string? _formErro;

    public CategoriasViewModel(CategoriaService svc, UsuarioService? usuarioService = null,
        PermissaoTelaService? permissaoTelaService = null)
    {
        _svc = svc;
        _usuarioService = usuarioService;
        _permissaoTelaService = permissaoTelaService;
    }

    private async Task AtualizarNivelTelaAsync()
    {
        var usuario = _usuarioService?.UsuarioAtual;
        if (usuario == null || _permissaoTelaService == null) return;
        var nivel = await _permissaoTelaService.ObterNivelEfetivoAsync(usuario, "categorias");
        PodeEditarTela  = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Editar);
        PodeExcluirTela = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Excluir);
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            await AtualizarNivelTelaAsync();
            var arvore = (await _svc.ObterArvoreAsync()).ToList();
            Categorias = new ObservableCollection<Categoria>(arvore);

            var raiz = (await _svc.ObterRaizAsync()).ToList();
            raiz.Insert(0, new Categoria { Id = 0, Nome = "— Nenhuma (categoria raiz) —" });
            CategoriasRaiz = new ObservableCollection<Categoria>(raiz);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public void NovaCategoria()
    {
        FormId = 0; FormNome = string.Empty; FormIcone = IconesDisponiveis[0];
        FormCor = "#6C5CE7"; FormPaiId = null; FormErro = null;
        FormTitulo = "Nova Categoria";
        MostrarFormulario = true;
    }

    [RelayCommand]
    public void EditarCategoria(Categoria cat)
    {
        FormId = cat.Id; FormNome = cat.Nome; FormIcone = string.IsNullOrWhiteSpace(cat.Icone) ? IconesDisponiveis[0] : cat.Icone;
        FormCor = cat.Cor ?? "#6C5CE7"; FormPaiId = cat.PaiId; FormErro = null;
        FormTitulo = "Editar Categoria";
        MostrarFormulario = true;
    }

    [RelayCommand]
    public void CancelarForm() => MostrarFormulario = false;

    [RelayCommand]
    public void SelecionarIcone(string icone) => FormIcone = icone;

    [RelayCommand]
    public async Task SalvarCategoriaAsync()
    {
        FormErro = null;
        var cat = new Categoria
        {
            Id    = FormId,
            Nome  = FormNome,
            Icone = FormIcone,
            Cor   = FormCor,
            PaiId = FormPaiId == 0 ? null : FormPaiId
        };
        var (ok, msg) = await _svc.SalvarAsync(cat);
        if (!ok) { FormErro = msg; return; }
        MostrarFormulario = false;
        await CarregarAsync();
        MostrarMensagem(msg, true);
    }

    [RelayCommand]
    public async Task ExcluirCategoriaAsync(Categoria cat)
    {
        var (ok, msg) = await _svc.ExcluirAsync(cat.Id);
        MostrarMensagem(msg, ok);
        if (ok) await CarregarAsync();
    }

    private void MostrarMensagem(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => Mensagem = null,
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}
