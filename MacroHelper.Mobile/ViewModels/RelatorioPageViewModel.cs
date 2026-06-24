using CommunityToolkit.Mvvm.ComponentModel;
using MacroHelper.Mobile.Models;
using MacroHelper.Mobile.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace MacroHelper.Mobile.ViewModels;

public partial class RelatorioPageViewModel : ObservableObject
{
    private readonly ApiService _api;
    [ObservableProperty] private ObservableCollection<LogMobile> _registros = new();
    [ObservableProperty] private string _totalHoje = "0";
    [ObservableProperty] private string _total     = "0";
    [ObservableProperty] private bool   _carregando = false;

    public RelatorioPageViewModel(ApiService api) => _api = api;

    public async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var lista = await _api.ObterLogAsync();
            Registros  = new ObservableCollection<LogMobile>(lista);
            Total      = lista.Count.ToString();
            TotalHoje  = lista.Count(l => l.DataUso.Date == DateTime.Today).ToString();
        }
        catch { }
        finally { Carregando = false; }
    }
}

public class LogMobile
{
    public string  MacroTitulo { get; set; } = string.Empty;
    public string  MacroAtalho { get; set; } = string.Empty;
    public string? Aplicativo  { get; set; }
    public DateTime DataUso    { get; set; }
}
