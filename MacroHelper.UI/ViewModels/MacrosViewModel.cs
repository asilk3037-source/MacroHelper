using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace MacroHelper.UI.ViewModels;

public partial class MacrosViewModel : ObservableObject
{
    private readonly MacroService     _macroService;
    private readonly CategoriaService _catService;
    private readonly IaService?       _iaService;
    private readonly GrupoService?    _grupoService;
    private readonly UsuarioService?  _usuarioService;
    private readonly PermissaoTelaService? _permissaoTelaService;
    private CancellationTokenSource? _buscaDebounceCts;

    public bool IsAdmin => _usuarioService?.UsuarioAtual?.Perfil == "Admin";

    [ObservableProperty] private bool _podeEditarTela  = true;
    [ObservableProperty] private bool _podeExcluirTela = true;

    [ObservableProperty] private ObservableCollection<Macro> _macros = new();
    [ObservableProperty] private ObservableCollection<string> _categorias = new();
    [ObservableProperty] private string  _termoBusca      = string.Empty;
    [ObservableProperty] private string  _categoriaFiltro = "Todas";
    [ObservableProperty] private bool    _isLoading       = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;
    [ObservableProperty] private bool    _mostrarFormulario = false;
    [ObservableProperty] private MacroFormViewModel? _formularioAtual;
    [ObservableProperty] private ObservableCollection<Macro> _pendentes = new();
    [ObservableProperty] private bool    _mostrarPendentes = false;
    [ObservableProperty] private bool    _somenteFavoritos = false;
    [ObservableProperty] private Macro?  _macroRejeitando;
    [ObservableProperty] private string  _motivoRejeicao = string.Empty;
    [ObservableProperty] private bool    _mostrarRejeicao = false;
    [ObservableProperty] private bool    _mostrarArquivadas = false;

    public MacrosViewModel(MacroService macroService, CategoriaService catService,
        IaService iaService, GrupoService? grupoService = null, UsuarioService? usuarioService = null,
        PermissaoTelaService? permissaoTelaService = null)
    {
        _macroService   = macroService;
        _catService     = catService;
        _iaService      = iaService;
        _grupoService   = grupoService;
        _usuarioService = usuarioService;
        _permissaoTelaService = permissaoTelaService;
    }

    private async Task AtualizarNivelTelaAsync()
    {
        var usuario = _usuarioService?.UsuarioAtual;
        if (usuario == null || _permissaoTelaService == null) return;
        var nivel = await _permissaoTelaService.ObterNivelEfetivoAsync(usuario, "macros");
        PodeEditarTela  = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Editar);
        PodeExcluirTela = NiveisPermissaoTela.Atende(nivel, NiveisPermissaoTela.Excluir);
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            await AtualizarNivelTelaAsync();
            IEnumerable<Macro> resultado;
            if (!string.IsNullOrWhiteSpace(TermoBusca))
                resultado = await _macroService.PesquisarAsync(TermoBusca);
            else if (CategoriaFiltro != "Todas")
                resultado = await _macroService.ObterPorCategoriaAsync(CategoriaFiltro);
            else
                resultado = await _macroService.ObterTodosAsync();

            if (SomenteFavoritos)
                resultado = resultado.Where(m => m.FavoritoPessoal);

            resultado = MostrarArquivadas ? resultado.Where(m => !m.Ativo) : resultado.Where(m => m.Ativo);

            Macros = new ObservableCollection<Macro>(resultado);

            var cats = await _macroService.ObterCategoriasAsync();
            Categorias = new ObservableCollection<string>(new[] { "Todas" }.Concat(cats));

            var pendentes = await _macroService.ObterPendentesAsync();
            Pendentes = new ObservableCollection<Macro>(pendentes);
        }
        finally { IsLoading = false; }
    }

    partial void OnSomenteFavoritosChanged(bool _) { var __ = CarregarAsync(); }
    partial void OnMostrarArquivadasChanged(bool _) { var __ = CarregarAsync(); }

    [RelayCommand]
    public void ToggleSomenteFavoritos() => SomenteFavoritos = !SomenteFavoritos;

    [RelayCommand]
    public void ToggleMostrarArquivadas() => MostrarArquivadas = !MostrarArquivadas;

    [RelayCommand]
    public async Task ArquivarMacro(Macro macro)
    {
        var (ok, msg) = await _macroService.ArquivarAsync(macro.Id, arquivar: true);
        MostrarMsg(msg, ok);
        if (ok) await CarregarAsync();
    }

    [RelayCommand]
    public async Task DesarquivarMacro(Macro macro)
    {
        var (ok, msg) = await _macroService.ArquivarAsync(macro.Id, arquivar: false);
        MostrarMsg(msg, ok);
        if (ok) await CarregarAsync();
    }

    /// <summary>Favorito pessoal (meu) — não afeta outros usuários.</summary>
    [RelayCommand]
    public async Task ToggleFavorito(Macro macro)
    {
        await _macroService.ToggleFavoritoPessoalAsync(macro.Id);
        await CarregarAsync();
    }

    /// <summary>Destaque de equipe — visível para todos. Somente admin.</summary>
    [RelayCommand]
    public async Task ToggleDestaqueEquipe(Macro macro)
    {
        await _macroService.ToggleFavoritoAsync(macro);
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task AprovarMacro(Macro macro)
    {
        var (ok, msg) = await _macroService.AprovarAsync(macro.Id);
        MostrarMsg(msg, ok);
        if (ok) await CarregarAsync();
    }

    [RelayCommand]
    public void AbrirRejeicao(Macro macro)
    {
        MacroRejeitando = macro;
        MotivoRejeicao  = string.Empty;
        MostrarRejeicao = true;
    }

    [RelayCommand]
    public void CancelarRejeicao() => MostrarRejeicao = false;

    [RelayCommand]
    public async Task ConfirmarRejeicaoAsync()
    {
        if (MacroRejeitando == null) return;
        var (ok, msg) = await _macroService.RejeitarAsync(MacroRejeitando.Id, MotivoRejeicao);
        MostrarRejeicao = false;
        MostrarMsg(msg, ok);
        if (ok) await CarregarAsync();
    }

    [RelayCommand]
    public void TogglePendentes() => MostrarPendentes = !MostrarPendentes;

    [RelayCommand]
    public async Task ExportarJson()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"macros_{DateTime.Now:yyyyMMdd}.json",
                Filter   = "JSON (*.json)|*.json"
            };
            if (dialog.ShowDialog() != true) return;

            var json = await _macroService.ExportarJsonAsync();
            await File.WriteAllTextAsync(dialog.FileName, json);
            MostrarMsg("Macros exportadas com sucesso!", true);
        }
        catch (Exception ex) { MostrarMsg($"Erro ao exportar: {ex.Message}", false); }
    }

    [RelayCommand]
    public async Task ImportarJson()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "JSON (*.json)|*.json" };
            if (dialog.ShowDialog() != true) return;

            var json = await File.ReadAllTextAsync(dialog.FileName);
            var (ok, msg, _, _) = await _macroService.ImportarJsonAsync(json);
            MostrarMsg(msg, ok);
            if (ok) await CarregarAsync();
        }
        catch (Exception ex) { MostrarMsg($"Erro ao importar: {ex.Message}", false); }
    }

    [RelayCommand]
    public async Task ImportarCsv()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "CSV (*.csv)|*.csv" };
            if (dialog.ShowDialog() != true) return;

            var csv = await File.ReadAllTextAsync(dialog.FileName);
            var (ok, msg, _, _) = await _macroService.ImportarCsvAsync(csv);
            MostrarMsg(msg, ok);
            if (ok) await CarregarAsync();
        }
        catch (Exception ex) { MostrarMsg($"Erro ao importar CSV: {ex.Message}", false); }
    }

    partial void OnTermoBuscaChanged(string _)      { var __ = DebounceCarregarAsync(); }
    partial void OnCategoriaFiltroChanged(string _) { var __ = CarregarAsync(); }

    /// <summary>Espera o usuário parar de digitar (300ms) antes de buscar — evita uma chamada ao banco por tecla.</summary>
    private async Task DebounceCarregarAsync()
    {
        _buscaDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _buscaDebounceCts = cts;
        try { await Task.Delay(300, cts.Token); }
        catch (TaskCanceledException) { return; }
        if (cts.IsCancellationRequested) return;
        await CarregarAsync();
    }

    [RelayCommand]
    public void NovaMacro()
    {
        var vm = new MacroFormViewModel(_macroService, _catService, _iaService, null, _grupoService);
        vm.Salvo     += async () => { MostrarFormulario = false; await CarregarAsync(); MostrarMsg("Macro criada com sucesso!", true); };
        vm.Cancelado += () => MostrarFormulario = false;
        FormularioAtual   = vm;
        MostrarFormulario = true;
    }

    private bool EhPropria(Macro macro) => macro.CriadoPorId == _usuarioService?.UsuarioAtual?.Id;

    [RelayCommand]
    public void EditarMacro(Macro macro)
    {
        if (!IsAdmin && !EhPropria(macro) && !Permissoes.Tem(_usuarioService?.UsuarioAtual, Permissoes.EditarMacrosDeOutros))
        {
            MostrarMsg("Você não tem permissão para editar macros de outros usuários.", false);
            return;
        }

        var vm = new MacroFormViewModel(_macroService, _catService, _iaService, macro, _grupoService);
        vm.Salvo     += async () => { MostrarFormulario = false; await CarregarAsync(); MostrarMsg("Macro atualizada!", true); };
        vm.Cancelado += () => MostrarFormulario = false;
        FormularioAtual   = vm;
        MostrarFormulario = true;
    }

    [RelayCommand]
    public async Task ExcluirMacro(Macro macro)
    {
        if (!IsAdmin && !EhPropria(macro) && !Permissoes.Tem(_usuarioService?.UsuarioAtual, Permissoes.ExcluirMacros))
        {
            MostrarMsg("Você não tem permissão para excluir macros de outros usuários.", false);
            return;
        }

        var (ok, msg) = await _macroService.ExcluirAsync(macro.Id);
        MostrarMsg(msg, ok);
        if (ok) await CarregarAsync();
    }

    [RelayCommand]
    public void CopiarConteudo(Macro macro)
    {
        System.Windows.Clipboard.SetText(macro.Conteudo);
        MostrarMsg("Conteúdo copiado!", true);
    }

    /// <summary>
    /// Abre um novo e-mail com o conteúdo da macro, usando o cliente de e-mail padrão do Windows
    /// (Outlook desktop, ou o app web configurado como padrão — incluindo Gmail). Não exige
    /// nenhuma integração de API/OAuth: usa o protocolo "mailto:" do próprio sistema operacional.
    /// </summary>
    [RelayCommand]
    public void EnviarPorEmail(Macro macro)
    {
        try
        {
            var assunto = Uri.EscapeDataString(macro.Titulo);
            var corpo   = Uri.EscapeDataString(macro.Conteudo);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                $"mailto:?subject={assunto}&body={corpo}") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MostrarMsg($"Não foi possível abrir o cliente de e-mail: {ex.Message}", false);
        }
    }

    private CancellationTokenSource? _msgCts;

    private void MostrarMsg(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        _msgCts?.Cancel();
        var cts = _msgCts = new CancellationTokenSource();
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500, cts.Token).ContinueWith(_ => Mensagem = null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}
