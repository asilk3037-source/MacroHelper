using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace MacroHelper.UI.ViewModels;

public partial class GrupoCheckItem : ObservableObject
{
    public int    Id   { get; }
    public string Nome { get; }
    [ObservableProperty] private bool _selecionado;
    public GrupoCheckItem(int id, string nome, bool selecionado) { Id = id; Nome = nome; Selecionado = selecionado; }
}

public partial class MacroFormViewModel : ObservableObject
{
    private readonly MacroService     _macroService;
    private readonly CategoriaService _catService;
    private readonly GrupoService?    _grupoService;
    private readonly IaService?       _iaService;
    private readonly Macro?           _original;

    [ObservableProperty] private string  _atalho    = string.Empty;
    [ObservableProperty] private string  _titulo    = string.Empty;
    [ObservableProperty] private string  _conteudo  = string.Empty;
    [ObservableProperty] private int?    _categoriaId;
    [ObservableProperty] private bool    _ativo     = true;
    [ObservableProperty] private string? _atalhoTecla;
    [ObservableProperty] private string? _erroMensagem;
    [ObservableProperty] private string? _avisoDuplicata;
    [ObservableProperty] private bool    _isSaving  = false;
    [ObservableProperty] private ObservableCollection<Categoria> _categorias = new();
    [ObservableProperty] private ObservableCollection<GrupoCheckItem> _grupos = new();

    // Realce de sintaxe (variáveis {nome} e condicionais {se campo=valor}...{fim}) detectados ao digitar
    [ObservableProperty] private ObservableCollection<string> _variaveisNoConteudo = new();
    [ObservableProperty] private ObservableCollection<string> _condicionaisNoConteudo = new();
    public bool HaTokensDetectados => VariaveisNoConteudo.Count > 0 || CondicionaisNoConteudo.Count > 0;
    private static readonly Regex _variavelRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);
    private static readonly Regex _condicionalRegex = new(@"\{se\s+\w+=[^}]+\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    partial void OnConteudoChanged(string value)
    {
        var condicionais = _condicionalRegex.Matches(value).Select(m => m.Value).Distinct().ToList();
        // Remove os trechos de condicionais antes de extrair variáveis simples, para não listar "{se...}" como variável.
        var semCondicionais = _condicionalRegex.Replace(value, " ");
        var variaveis = _variavelRegex.Matches(semCondicionais)
            .Select(m => "{" + m.Groups[1].Value + "}")
            .Where(v => !v.Equals("{fim}", StringComparison.OrdinalIgnoreCase))
            .Distinct().ToList();

        VariaveisNoConteudo    = new ObservableCollection<string>(variaveis);
        CondicionaisNoConteudo = new ObservableCollection<string>(condicionais);
        OnPropertyChanged(nameof(HaTokensDetectados));
    }

    // Anexo de imagem (ex: assinatura)
    [ObservableProperty] private string? _imagemBase64;
    [ObservableProperty] private bool    _temImagem;

    // Histórico de versões
    [ObservableProperty] private ObservableCollection<MacroVersao> _historico = new();
    [ObservableProperty] private bool _mostrarHistorico = false;

    // IA
    [ObservableProperty] private bool   _mostrarPainelIA  = false;
    [ObservableProperty] private string _descricaoIA      = string.Empty;
    [ObservableProperty] private string _tomIA            = "profissional";
    [ObservableProperty] private bool   _gerandoIA        = false;
    [ObservableProperty] private string _iaStatusMsg      = string.Empty;

    public bool   IsEdicao         => _original != null;
    public string TituloFormulario => IsEdicao ? "Editar Macro" : "Nova Macro";
    public bool   IaDisponivel     => _iaService != null && !string.IsNullOrEmpty(_iaService.ApiKey);

    // Templates prontos por setor — só fazem sentido ao criar uma macro nova
    public bool MostrarTemplates => !IsEdicao;
    public MacroTemplate[] TemplatesDisponiveis => MacroTemplates.Todos;

    [RelayCommand]
    public void AplicarTemplate(MacroTemplate template)
    {
        if (!string.IsNullOrWhiteSpace(Conteudo) || !string.IsNullOrWhiteSpace(Titulo))
        {
            var resposta = System.Windows.MessageBox.Show(
                "Isso vai substituir o título, atalho e conteúdo já preenchidos. Continuar?",
                "Aplicar template", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (resposta != System.Windows.MessageBoxResult.Yes) return;
        }

        Titulo   = template.Titulo;
        Atalho   = template.Atalho;
        Conteudo = template.Conteudo;
    }

    public event Func<Task>? Salvo;
    public event Action?     Cancelado;

    public MacroFormViewModel(MacroService macroService, CategoriaService catService,
        IaService? iaService, Macro? macro, GrupoService? grupoService = null)
    {
        _macroService = macroService;
        _catService   = catService;
        _iaService    = iaService;
        _original     = macro;
        _grupoService = grupoService;

        if (macro != null)
        {
            Atalho       = macro.Atalho;
            Titulo       = macro.Titulo;
            Conteudo     = macro.Conteudo;
            CategoriaId  = macro.CategoriaId;
            Ativo        = macro.Ativo;
            AtalhoTecla  = macro.AtalhoTecla;
            ImagemBase64 = macro.ImagemBase64;
            TemImagem    = macro.ImagemBase64 != null;
        }
        _ = CarregarCategoriasAsync();
        _ = CarregarGruposAsync();
    }

    private async Task CarregarCategoriasAsync()
    {
        var lista  = (await _catService.ObterTodosAsync()).ToList();
        lista.Insert(0, new Categoria { Id = 0, Nome = "— Sem categoria —" });
        Categorias  = new ObservableCollection<Categoria>(lista);
        if (CategoriaId == null) CategoriaId = 0;
    }

    private async Task CarregarGruposAsync()
    {
        if (_grupoService == null) return;
        var permitidos = (_original?.GruposPermitidos ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse).ToHashSet();
        var lista = await _grupoService.ObterTodosAsync();
        Grupos = new ObservableCollection<GrupoCheckItem>(
            lista.Select(g => new GrupoCheckItem(g.Id, g.Nome, permitidos.Contains(g.Id))));
    }

    [RelayCommand]
    public void ColarImagem()
    {
        try
        {
            if (!System.Windows.Clipboard.ContainsImage()) return;
            var img = System.Windows.Clipboard.GetImage();
            using var ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(img));
            encoder.Save(ms);
            ImagemBase64 = Convert.ToBase64String(ms.ToArray());
            TemImagem = true;
        }
        catch { }
    }

    [RelayCommand]
    public void RemoverImagem()
    {
        ImagemBase64 = null;
        TemImagem = false;
    }

    [RelayCommand]
    public async Task AbrirHistorico()
    {
        if (_original == null) return;
        Historico = new ObservableCollection<MacroVersao>(await _macroService.ObterVersoesAsync(_original.Id));
        MostrarHistorico = true;
    }

    [RelayCommand] public void FecharHistorico() => MostrarHistorico = false;

    [RelayCommand]
    public void RestaurarVersao(MacroVersao versao)
    {
        Titulo = versao.Titulo;
        Conteudo = versao.Conteudo;
        MostrarHistorico = false;
    }

    // ── IA: Gerar conteúdo por descrição ──────────────────────
    [RelayCommand]
    public async Task GerarComIAAsync()
    {
        if (_iaService == null || string.IsNullOrWhiteSpace(DescricaoIA)) return;
        GerandoIA = true; IaStatusMsg = "Gerando conteúdo...";
        try
        {
            Conteudo       = await _iaService.GerarConteudoMacroAsync(DescricaoIA, TomIA);
            MostrarPainelIA = false;
            IaStatusMsg    = "Conteúdo gerado.";
            await Task.Delay(2000);
            IaStatusMsg = string.Empty;
        }
        catch (Exception ex) { IaStatusMsg = $"Erro: {ex.Message}"; }
        finally { GerandoIA = false; }
    }

    // ── IA: Ajustar tom ───────────────────────────────────────
    [RelayCommand]
    public async Task AjustarTomAsync(string tom)
    {
        if (_iaService == null || string.IsNullOrWhiteSpace(Conteudo)) return;
        GerandoIA = true; IaStatusMsg = $"Ajustando tom ({tom})...";
        try
        {
            Conteudo    = await _iaService.AjustarTomAsync(Conteudo, tom);
            IaStatusMsg = $"Tom ajustado: {tom}.";
            await Task.Delay(2000);
            IaStatusMsg = string.Empty;
        }
        catch (Exception ex) { IaStatusMsg = $"Erro: {ex.Message}"; }
        finally { GerandoIA = false; }
    }

    // ── IA: Sugerir atalho ────────────────────────────────────
    [RelayCommand]
    public async Task SugerirAtalhoAsync()
    {
        if (_iaService == null || string.IsNullOrWhiteSpace(Titulo)) return;
        GerandoIA = true; IaStatusMsg = "Sugerindo atalho...";
        try
        {
            Atalho      = await _iaService.SugerirAtalhoAsync(Titulo);
            IaStatusMsg = "Atalho sugerido.";
            await Task.Delay(2000);
            IaStatusMsg = string.Empty;
        }
        catch (Exception ex) { IaStatusMsg = $"Erro: {ex.Message}"; }
        finally { GerandoIA = false; }
    }

    [RelayCommand] public void AbrirIA()   => MostrarPainelIA = true;
    [RelayCommand] public void FecharIA()  => MostrarPainelIA = false;

    // ── Salvar ────────────────────────────────────────────────
    [RelayCommand]
    public async Task Salvar()
    {
        ErroMensagem = null; IsSaving = true;
        try
        {
            var gruposSelecionados = Grupos.Where(g => g.Selecionado).Select(g => g.Id).ToList();
            var macro = new Macro
            {
                Id          = _original?.Id ?? 0,
                Atalho      = Atalho,
                Titulo      = Titulo,
                Conteudo    = Conteudo,
                CategoriaId = CategoriaId == 0 ? null : CategoriaId,
                Categoria   = Categorias.FirstOrDefault(c => c.Id == CategoriaId)?.Nome,
                Ativo       = Ativo,
                Favorito    = _original?.Favorito ?? false,
                AtalhoTecla = string.IsNullOrWhiteSpace(AtalhoTecla) ? null : AtalhoTecla.Trim(),
                ImagemBase64 = ImagemBase64,
                GruposPermitidos = gruposSelecionados.Count == 0 ? null : string.Join(",", gruposSelecionados)
            };

            if (AvisoDuplicata == null)
            {
                var duplicata = await _macroService.DetectarPossivelDuplicataAsync(macro);
                if (duplicata != null)
                {
                    AvisoDuplicata = $"Conteúdo muito parecido com \"{duplicata.Titulo}\" (/{duplicata.Atalho}). Clique em Salvar novamente para confirmar.";
                    return;
                }
            }

            var (ok, msg, _) = await _macroService.SalvarAsync(macro);
            if (!ok) { ErroMensagem = msg; return; }
            if (Salvo != null) await Salvo();
        }
        finally { IsSaving = false; }
    }

    [RelayCommand] public void Cancelar() => Cancelado?.Invoke();
}
