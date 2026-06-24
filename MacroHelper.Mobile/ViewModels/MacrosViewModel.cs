using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Mobile.Models;
using MacroHelper.Mobile.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.Mobile.ViewModels;

public partial class MacrosViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private ObservableCollection<MacroMobile> _macros = new();
    [ObservableProperty] private string  _busca      = string.Empty;
    [ObservableProperty] private bool    _carregando = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _modoOffline = false;

    // Set by MacrosPage (navigation commands need Shell access)
    public Command<MacroMobile>? EditarCommand   { get; set; }
    public Command?              NovaMacroCommand { get; set; }

    public MacrosViewModel(ApiService api) => _api = api;

    public async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var lista = await _api.ObterMacrosAsync(
                string.IsNullOrWhiteSpace(Busca) ? null : Busca);
            Macros = new ObservableCollection<MacroMobile>(lista);
            ModoOffline = _api.UltimaCargaOffline;
            if (ModoOffline) Mensagem = "Sem conexão — mostrando macros salvas localmente.";
        }
        catch (Exception ex) { Mensagem = $"Erro: {ex.Message}"; }
        finally { Carregando = false; }
    }

    partial void OnBuscaChanged(string _) => _ = CarregarAsync();

    [RelayCommand]
    public async Task CopiarAsync(MacroMobile macro)
    {
        await Clipboard.SetTextAsync(macro.Conteudo);
        await _api.RegistrarUsoAsync(macro.Id, macro.Titulo, macro.Atalho);
        Mensagem = "Copiado! Cole onde precisar.";
        await Task.Delay(3000);
        Mensagem = null;
    }

    [RelayCommand]
    public async Task ExcluirAsync(MacroMobile macro)
    {
        var ok = await _api.ExcluirMacroAsync(macro.Id);
        if (ok) await CarregarAsync();
    }

    [RelayCommand]
    public async Task AtualizarAsync() => await CarregarAsync();
}
