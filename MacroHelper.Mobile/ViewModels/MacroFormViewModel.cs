using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Mobile.Models;
using MacroHelper.Mobile.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.Mobile.ViewModels;

public partial class MacroFormViewModel : ObservableObject
{
    private readonly ApiService  _api;
    private MacroMobile? _original;

    [ObservableProperty] private string  _atalho    = string.Empty;
    [ObservableProperty] private string  _titulo    = string.Empty;
    [ObservableProperty] private string  _conteudo  = string.Empty;
    [ObservableProperty] private bool    _ativo     = true;
    [ObservableProperty] private string? _erro;
    [ObservableProperty] private bool    _salvando  = false;
    [ObservableProperty] private bool    _gerandoIA = false;
    [ObservableProperty] private string  _descricaoIA = string.Empty;
    [ObservableProperty] private string  _tomIA     = "profissional";
    [ObservableProperty] private bool    _mostrarIA = false;
    [ObservableProperty] private ObservableCollection<CategoriaMobile> _categorias = new();
    [ObservableProperty] private CategoriaMobile? _categoriaSelecionada;

    public bool IsEdicao => _original?.Id > 0;
    public string Titulo2 => IsEdicao ? "Editar Macro" : "Nova Macro";

    public event Action? Salvo;
    public event Action? Cancelado;

    public MacroFormViewModel(ApiService api) => _api = api;

    public async Task InicializarAsync(MacroMobile? macro = null)
    {
        _original = macro;
        if (macro != null)
        {
            Atalho   = macro.Atalho;
            Titulo   = macro.Titulo;
            Conteudo = macro.Conteudo;
            Ativo    = macro.Ativo;
        }
        var cats = await _api.ObterCategoriasAsync();
        Categorias = new ObservableCollection<CategoriaMobile>(cats);
    }

    [RelayCommand]
    public async Task SalvarAsync()
    {
        Erro = null; Salvando = true;
        try
        {
            var macro = new MacroMobile
            {
                Id          = _original?.Id ?? 0,
                Atalho      = Atalho, Titulo = Titulo,
                Conteudo    = Conteudo, Ativo = Ativo,
                CategoriaId = CategoriaSelecionada?.Id,
                Categoria   = CategoriaSelecionada?.Nome
            };
            var saved = await _api.SalvarMacroAsync(macro);
            if (saved == null) { Erro = "Erro ao salvar."; return; }
            Salvo?.Invoke();
        }
        finally { Salvando = false; }
    }

    [RelayCommand]
    public async Task GerarComIAAsync()
    {
        if (string.IsNullOrWhiteSpace(DescricaoIA)) return;
        GerandoIA = true;
        try
        {
            Conteudo = await _api.GerarComIAAsync(DescricaoIA, TomIA);
            MostrarIA = false;
        }
        finally { GerandoIA = false; }
    }

    [RelayCommand]
    public async Task SugerirAtalhoAsync()
    {
        if (string.IsNullOrWhiteSpace(Titulo)) return;
        GerandoIA = true;
        try { Atalho = await _api.GerarComIAAsync($"Sugira apenas um atalho para: {Titulo}", "atalho"); }
        finally { GerandoIA = false; }
    }

    [RelayCommand]
    public async Task AjustarTomAsync(string tom)
    {
        if (string.IsNullOrWhiteSpace(Conteudo)) return;
        GerandoIA = true;
        try
        {
            // Use IA to rewrite the current content with the chosen tone
            var prompt = $"Reescreva este texto de forma {tom}:\n\n{Conteudo}";
            Conteudo = await _api.GerarComIAAsync(prompt, tom);
        }
        finally { GerandoIA = false; }
    }

    [RelayCommand] public void AbrirIA()    => MostrarIA = true;
    [RelayCommand] public void FecharIA()   => MostrarIA = false;
    [RelayCommand] public void Cancelar()  => Cancelado?.Invoke();
}
