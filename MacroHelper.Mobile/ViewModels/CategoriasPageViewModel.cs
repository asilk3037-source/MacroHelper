using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Mobile.Models;
using MacroHelper.Mobile.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.Mobile.ViewModels;

public partial class CategoriasPageViewModel : ObservableObject
{
    private readonly ApiService _api;
    [ObservableProperty] private ObservableCollection<CategoriaMobile> _categorias = new();
    [ObservableProperty] private bool _carregando = false;

    public Command<CategoriaMobile>? EditarCommand  { get; set; }
    public Command<CategoriaMobile>? ExcluirCommand { get; set; }
    public Command? NovaCategoriaCommand            { get; set; }

    public CategoriasPageViewModel(ApiService api) => _api = api;

    public async Task CarregarAsync()
    {
        Carregando = true;
        try { Categorias = new ObservableCollection<CategoriaMobile>(await _api.ObterCategoriasAsync()); }
        finally { Carregando = false; }
    }
}
