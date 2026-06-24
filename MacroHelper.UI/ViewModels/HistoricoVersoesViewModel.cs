using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class HistoricoVersoesViewModel : ObservableObject
{
    private readonly MacroService _macroService;

    [ObservableProperty] private ObservableCollection<Macro> _macros = new();
    [ObservableProperty] private Macro? _macroSelecionada;
    [ObservableProperty] private ObservableCollection<MacroVersao> _versoes = new();
    [ObservableProperty] private MacroVersao? _versaoEsquerda;
    [ObservableProperty] private MacroVersao? _versaoDireita;
    [ObservableProperty] private ObservableCollection<LinhaDiff> _diff = new();
    [ObservableProperty] private bool _isLoading = false;

    public HistoricoVersoesViewModel(MacroService macroService) => _macroService = macroService;

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try { Macros = new ObservableCollection<Macro>(await _macroService.ObterTodosAsync()); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task SelecionarMacroAsync(Macro macro)
    {
        MacroSelecionada = macro;
        Versoes = new ObservableCollection<MacroVersao>(await _macroService.ObterVersoesAsync(macro.Id));
        VersaoEsquerda = Versoes.LastOrDefault();
        VersaoDireita  = new MacroVersao { Titulo = macro.Titulo, Conteudo = macro.Conteudo, DataModificacao = macro.DataAtualizacao ?? macro.DataCriacao };
        AtualizarDiff();
    }

    [RelayCommand]
    public void SelecionarEsquerda(MacroVersao v) { VersaoEsquerda = v; AtualizarDiff(); }

    [RelayCommand]
    public void SelecionarDireita(MacroVersao v) { VersaoDireita = v; AtualizarDiff(); }

    private void AtualizarDiff()
    {
        var esquerda = (VersaoEsquerda?.Conteudo ?? string.Empty).Split('\n');
        var direita  = (VersaoDireita?.Conteudo ?? string.Empty).Split('\n');
        var max = Math.Max(esquerda.Length, direita.Length);
        var linhas = new List<LinhaDiff>();

        for (var i = 0; i < max; i++)
        {
            var le = i < esquerda.Length ? esquerda[i] : string.Empty;
            var ld = i < direita.Length  ? direita[i]  : string.Empty;
            linhas.Add(new LinhaDiff(i + 1, le, ld, le != ld));
        }
        Diff = new ObservableCollection<LinhaDiff>(linhas);
    }
}

public record LinhaDiff(int Numero, string Esquerda, string Direita, bool Diferente);
