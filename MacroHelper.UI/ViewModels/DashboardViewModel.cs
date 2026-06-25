using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MacroHelper.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly MacroService   _macroService;
    private readonly LogUsoService   _logService;
    private readonly UsuarioService  _usuarioService;

    [ObservableProperty] private string _nomeUsuario       = string.Empty;
    [ObservableProperty] private string _hookStatusTexto   = string.Empty;
    [ObservableProperty] private int    _totalMacros       = 0;
    [ObservableProperty] private int    _macrosNovasSemana = 0;
    [ObservableProperty] private int    _usadasHoje        = 0;
    [ObservableProperty] private string _minutosEconomizados = "0min";
    [ObservableProperty] private bool   _isLoading         = false;

    [ObservableProperty] private ObservableCollection<RankingItem> _maisUsadas = new();
    [ObservableProperty] private ObservableCollection<AtividadeItem> _atividadeRecente = new();

    /// <summary>Disparado pelo botão "Nova macro" do hero — MainViewModel decide a navegação.</summary>
    public event EventHandler? NovaMacroSolicitada;

    public DashboardViewModel(MacroService macroService, LogUsoService logService, UsuarioService usuarioService)
    {
        _macroService   = macroService;
        _logService     = logService;
        _usuarioService = usuarioService;
        NomeUsuario = _usuarioService.UsuarioAtual?.Nome ?? "Usuário";

        var dia = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek);
        HookStatusTexto = $"{char.ToUpper(dia[0])}{dia[1..]}, hook ativo";
    }

    [RelayCommand]
    public void NovaMacro() => NovaMacroSolicitada?.Invoke(this, EventArgs.Empty);

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var macros = (await _macroService.ObterTodosAsync()).ToList();
            TotalMacros = macros.Count;
            MacrosNovasSemana = macros.Count(m => m.DataCriacao >= DateTime.Now.AddDays(-7));
            UsadasHoje  = await _logService.TotalHojeAsync();

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var top = await _logService.ObterTopMacrosAsync(inicioMes, DateTime.Now.AddDays(1), 5);
            MaisUsadas = new ObservableCollection<RankingItem>(
                top.Select(t => new RankingItem(t.Titulo, t.Atalho, t.Total)));

            var minutos = await _logService.EstimarMinutosEconomizadosAsync(inicioMes, DateTime.Now.AddDays(1));
            MinutosEconomizados = minutos >= 60 ? $"{minutos / 60:0.#}h" : $"{minutos:0}min";

            var recentes = (await _logService.ObterRecentesAsync(8)).ToList();
            AtividadeRecente = new ObservableCollection<AtividadeItem>(
                recentes.Select(r => new AtividadeItem(r.MacroTitulo, r.MacroAtalho, r.DataUso)));
        }
        finally { IsLoading = false; }
    }
}

public record AtividadeItem(string Titulo, string Atalho, DateTime Data);
