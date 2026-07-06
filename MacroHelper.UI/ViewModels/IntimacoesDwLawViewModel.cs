using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

// ── Item de exibição ─────────────────────────────────────────────────────────
public partial class IntimacaoItem : ObservableObject
{
    public IntimacaoErro Entidade { get; }

    [ObservableProperty] private string _status;
    [ObservableProperty] private string _observacao;
    [ObservableProperty] private bool   _editandoObs;

    public IntimacaoItem(IntimacaoErro e)
    {
        Entidade    = e;
        _status     = e.Status;
        _observacao = e.Observacao;
    }

    public int      Id           => Entidade.Id;
    public string   DataFormatada => Entidade.DataEmail.ToString("dd/MM");
    public string   Advogado     => Entidade.Advogado;
    public string   TipoLogin    => Entidade.TipoLogin;
    public string   Sistema      => Entidade.Sistema;
    public string   LinkTribunal => Entidade.LinkTribunal;
    public string   ErroTexto    => Entidade.Erro;
    public int      DiasAberto   => (int)(DateTime.Today - Entidade.DataEmail.Date).TotalDays;

    public bool     IsNovo        => Status == "Novo";
    public bool     IsRecorrente  => Status == "Recorrente";
    public bool     IsEmAndamento => Status == "Em andamento";
    public bool     IsPendenteCliente => Status == "Pendente cliente";
    public bool     IsResolvido   => Status == "Resolvido";

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(IsNovo));
        OnPropertyChanged(nameof(IsRecorrente));
        OnPropertyChanged(nameof(IsEmAndamento));
        OnPropertyChanged(nameof(IsPendenteCliente));
        OnPropertyChanged(nameof(IsResolvido));
        Entidade.Status = value;
    }

    partial void OnObservacaoChanged(string value) => Entidade.Observacao = value;
}

// ── ViewModel principal ──────────────────────────────────────────────────────
public partial class IntimacoesDwLawViewModel : ObservableObject
{
    private readonly IntimacoesDwLawService _svc;

    private List<IntimacaoItem> _todosItens = [];

    public ObservableCollection<IntimacaoItem>  Itens       { get; } = [];
    public ObservableCollection<IntimacaoItem>  Preview     { get; } = [];
    public ObservableCollection<string>         StatusOpcoes { get; } =
        new(["Novo", "Recorrente", "Em andamento", "Pendente cliente", "Resolvido"]);

    [ObservableProperty] private bool   _painel;           // painel de import visível?
    [ObservableProperty] private string _textoEmail  = string.Empty;
    [ObservableProperty] private DateTime _dataEmail = DateTime.Today;
    [ObservableProperty] private string _filtroStatus = "Todos";
    [ObservableProperty] private string _filtroTexto  = string.Empty;
    [ObservableProperty] private bool   _carregando;
    [ObservableProperty] private bool   _importando;
    [ObservableProperty] private string _msgExtracao = string.Empty;
    [ObservableProperty] private bool   _temPreview;

    public ObservableCollection<string> FiltroOpcoes { get; } =
        new(["Todos", "Novo", "Recorrente", "Em andamento", "Pendente cliente", "Resolvido"]);

    public IntimacoesDwLawViewModel(IntimacoesDwLawService svc)
    {
        _svc = svc;
        Preview.CollectionChanged += (_, _) => TemPreview = Preview.Count > 0;
    }

    public async Task CarregarAsync()
    {
        Carregando = true;
        try
        {
            var lista = await _svc.ObterTodosAsync();
            _todosItens = lista.Select(e => new IntimacaoItem(e)).ToList();
            AplicarFiltro();
        }
        finally { Carregando = false; }
    }

    [RelayCommand]
    private void AbrirPainel()
    {
        TextoEmail  = string.Empty;
        DataEmail   = DateTime.Today;
        MsgExtracao = string.Empty;
        Preview.Clear();
        Painel = true;
    }

    [RelayCommand]
    private void FecharPainel()
    {
        Painel = false;
        Preview.Clear();
    }

    [RelayCommand]
    private void Extrair()
    {
        MsgExtracao = string.Empty;
        Preview.Clear();

        if (string.IsNullOrWhiteSpace(TextoEmail))
        {
            MsgExtracao = "Cole o conteúdo do email antes de extrair.";
            return;
        }

        var extraidos = _svc.Extrair(TextoEmail, DataEmail);
        if (extraidos.Count == 0)
        {
            MsgExtracao = "Nenhum item encontrado. Verifique se o texto contém a tabela do email.";
            return;
        }

        foreach (var e in extraidos)
            Preview.Add(new IntimacaoItem(e));

        MsgExtracao = $"{extraidos.Count} item(ns) extraído(s) — confira e clique em Importar.";
    }

    [RelayCommand]
    private async Task ImportarLote()
    {
        if (Preview.Count == 0) return;
        Importando = true;
        try
        {
            var entidades = Preview.Select(i => i.Entidade).ToList();
            var total = await _svc.ImportarLoteAsync(entidades);
            Painel = false;
            Preview.Clear();
            await CarregarAsync();
            MsgExtracao = string.Empty;
        }
        finally { Importando = false; }
    }

    [RelayCommand]
    private async Task AlterarStatus(IntimacaoItem item)
    {
        await _svc.AtualizarStatusAsync(item.Entidade, item.Status);
        AplicarFiltro();
    }

    [RelayCommand]
    private async Task SalvarObs(IntimacaoItem item)
    {
        item.EditandoObs = false;
        await _svc.SalvarObservacaoAsync(item.Entidade);
    }

    [RelayCommand]
    private async Task Excluir(IntimacaoItem item)
    {
        await _svc.ExcluirAsync(item.Entidade);
        _todosItens.Remove(item);
        AplicarFiltro();
    }

    [RelayCommand]
    private void AbrirLink(IntimacaoItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.LinkTribunal))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = item.LinkTribunal,
                UseShellExecute = true
            });
    }

    partial void OnFiltroStatusChanged(string value) => AplicarFiltro();
    partial void OnFiltroTextoChanged(string value)  => AplicarFiltro();

    private void AplicarFiltro()
    {
        IEnumerable<IntimacaoItem> q = _todosItens;

        if (FiltroStatus != "Todos")
            q = q.Where(i => i.Status == FiltroStatus);

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
        {
            var t = FiltroTexto.Trim();
            q = q.Where(i =>
                i.Advogado.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                i.Sistema.Contains(t, StringComparison.OrdinalIgnoreCase)  ||
                i.ErroTexto.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        Itens.Clear();
        foreach (var i in q)
            Itens.Add(i);
    }
}
