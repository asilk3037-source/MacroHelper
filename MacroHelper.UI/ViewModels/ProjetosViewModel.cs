using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class ProjetosViewModel : ObservableObject
{
    private readonly ProjetoService  _projetoSvc;
    private readonly AtaService      _ataSvc;
    private readonly PendenciaService _pendSvc;

    [ObservableProperty] private ObservableCollection<Projeto>   _projetos       = new();
    [ObservableProperty] private ObservableCollection<Ata>       _atas           = new();
    [ObservableProperty] private ObservableCollection<Pendencia> _pendencias     = new();
    [ObservableProperty] private ObservableCollection<Pendencia> _pendenciasFiltradas = new();

    [ObservableProperty] private Projeto?   _projetoSelecionado;
    [ObservableProperty] private Ata?       _ataSelecionada;
    [ObservableProperty] private Pendencia? _pendenciaSelecionada;

    [ObservableProperty] private bool   _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private string  _filtroStatus = "Todas";

    // Formulário inline de projeto
    [ObservableProperty] private bool   _editandoProjeto = false;
    [ObservableProperty] private string _projetoNomeEdit = string.Empty;
    [ObservableProperty] private string _projetoDescEdit = string.Empty;
    [ObservableProperty] private string _projetoStatusEdit = "Ativo";

    // Formulário inline de ata
    [ObservableProperty] private bool     _editandoAta   = false;
    [ObservableProperty] private string   _ataTituloEdit = string.Empty;
    [ObservableProperty] private DateTime _ataDataEdit   = DateTime.Today;
    [ObservableProperty] private string   _ataHorarioEdit = string.Empty;
    [ObservableProperty] private string   _ataNotasEdit  = string.Empty;

    // Formulário inline de pendência
    [ObservableProperty] private bool     _editandoPendencia    = false;
    [ObservableProperty] private string   _pendDescricaoEdit    = string.Empty;
    [ObservableProperty] private string   _pendResponsavelEdit  = string.Empty;
    [ObservableProperty] private DateTime? _pendPrazoEdit       = null;
    [ObservableProperty] private string   _pendPrioridadeEdit   = "Media";

    public string[] StatusOptions    => new[] { "Todas", "Aberta", "Em andamento", "Concluída", "Cancelada" };
    public string[] PrioridadeOptions => new[] { "Alta", "Media", "Baixa" };
    public string[] ProjetoStatusOptions => new[] { "Ativo", "Concluído", "Cancelado" };

    public ProjetosViewModel(ProjetoService projetoSvc, AtaService ataSvc, PendenciaService pendSvc)
    {
        _projetoSvc = projetoSvc;
        _ataSvc     = ataSvc;
        _pendSvc    = pendSvc;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = await _projetoSvc.ObterTodosAsync();
            Projetos = new ObservableCollection<Projeto>(lista);
            if (ProjetoSelecionado == null && Projetos.Count > 0)
                await SelecionarProjetoAsync(Projetos[0]);
        }
        finally { IsLoading = false; }
    }

    private async Task SelecionarProjetoAsync(Projeto p)
    {
        ProjetoSelecionado = p;
        AtaSelecionada     = null;
        Atas               = new();
        Pendencias         = new();
        PendenciasFiltradas = new();
        EditandoProjeto    = false;
        EditandoAta        = false;
        EditandoPendencia  = false;

        var atas = await _ataSvc.ObterPorProjetoAsync(p.Id);
        Atas = new ObservableCollection<Ata>(atas);

        var pends = await _pendSvc.ObterPorProjetoAsync(p.Id);
        Pendencias = new ObservableCollection<Pendencia>(pends);
        AplicarFiltro();
    }

    partial void OnFiltroStatusChanged(string _) => AplicarFiltro();

    private void AplicarFiltro()
    {
        var lista = FiltroStatus == "Todas"
            ? Pendencias
            : new ObservableCollection<Pendencia>(Pendencias.Where(p => p.Status == FiltroStatus));
        PendenciasFiltradas = lista;
    }

    // ── Navegação ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelecionarProjeto(Projeto p)
    {
        if (p == null) return;
        await SelecionarProjetoAsync(p);
    }

    [RelayCommand]
    private async Task SelecionarAta(Ata a)
    {
        AtaSelecionada    = a;
        EditandoAta       = false;
        EditandoPendencia = false;
        if (a == null)
        {
            AplicarFiltro();
            return;
        }
        var pends = await _pendSvc.ObterPorProjetoAsync(ProjetoSelecionado!.Id);
        Pendencias = new ObservableCollection<Pendencia>(pends.Where(p => p.AtaId == a.Id));
        AplicarFiltro();
    }

    // ── CRUD Projeto ──────────────────────────────────────────────

    [RelayCommand]
    private void NovoProjeto()
    {
        ProjetoNomeEdit  = string.Empty;
        ProjetoDescEdit  = string.Empty;
        ProjetoStatusEdit = "Ativo";
        EditandoProjeto  = true;
    }

    [RelayCommand]
    private async Task SalvarProjeto()
    {
        if (string.IsNullOrWhiteSpace(ProjetoNomeEdit)) return;

        if (ProjetoSelecionado != null && ProjetoSelecionado.Id > 0 && EditandoProjeto
            && ProjetoSelecionado.Nome != string.Empty && ProjetoNomeEdit == ProjetoSelecionado.Nome)
        {
            // Editando existente
            ProjetoSelecionado.Nome      = ProjetoNomeEdit;
            ProjetoSelecionado.Descricao = ProjetoDescEdit;
            ProjetoSelecionado.Status    = ProjetoStatusEdit;
            var (ok, msg) = await _projetoSvc.SalvarAsync(ProjetoSelecionado);
            Mensagem = msg;
        }
        else
        {
            // Novo
            var novo = new Projeto { Nome = ProjetoNomeEdit, Descricao = ProjetoDescEdit, Status = ProjetoStatusEdit };
            var (ok, msg, id) = await _projetoSvc.CriarAsync(novo);
            Mensagem = msg;
            if (ok) { novo.Id = id; Projetos.Insert(0, novo); await SelecionarProjetoAsync(novo); }
        }
        EditandoProjeto = false;
    }

    [RelayCommand] private void CancelarProjeto() => EditandoProjeto = false;

    [RelayCommand]
    private void EditarProjeto()
    {
        if (ProjetoSelecionado == null) return;
        ProjetoNomeEdit  = ProjetoSelecionado.Nome;
        ProjetoDescEdit  = ProjetoSelecionado.Descricao ?? string.Empty;
        ProjetoStatusEdit = ProjetoSelecionado.Status;
        EditandoProjeto  = true;
    }

    [RelayCommand]
    private async Task ExcluirProjeto()
    {
        if (ProjetoSelecionado == null) return;
        await _projetoSvc.ExcluirAsync(ProjetoSelecionado.Id);
        Projetos.Remove(ProjetoSelecionado);
        ProjetoSelecionado = null; Atas = new(); Pendencias = new(); PendenciasFiltradas = new();
        if (Projetos.Count > 0) await SelecionarProjetoAsync(Projetos[0]);
    }

    // ── CRUD Ata ──────────────────────────────────────────────────

    [RelayCommand]
    private void NovaAta()
    {
        AtaTituloEdit  = string.Empty;
        AtaDataEdit    = DateTime.Today;
        AtaHorarioEdit = string.Empty;
        AtaNotasEdit   = string.Empty;
        EditandoAta    = true;
    }

    [RelayCommand]
    private async Task SalvarAta()
    {
        if (ProjetoSelecionado == null || string.IsNullOrWhiteSpace(AtaTituloEdit)) return;
        var nova = new Ata
        {
            ProjetoId    = ProjetoSelecionado.Id,
            Titulo       = AtaTituloEdit,
            DataReuniao  = AtaDataEdit,
            Horario      = AtaHorarioEdit,
            Notas        = AtaNotasEdit
        };
        var (ok, msg, id) = await _ataSvc.CriarAsync(nova);
        Mensagem = msg;
        if (ok) { nova.Id = id; Atas.Insert(0, nova); }
        EditandoAta = false;
    }

    [RelayCommand] private void CancelarAta() => EditandoAta = false;

    [RelayCommand]
    private async Task ExcluirAta(Ata a)
    {
        if (a == null) return;
        await _ataSvc.ExcluirAsync(a.Id);
        Atas.Remove(a);
        if (AtaSelecionada?.Id == a.Id) { AtaSelecionada = null; AplicarFiltro(); }
    }

    // ── CRUD Pendência ────────────────────────────────────────────

    [RelayCommand]
    private void NovaPendencia()
    {
        PendDescricaoEdit   = string.Empty;
        PendResponsavelEdit = string.Empty;
        PendPrazoEdit       = null;
        PendPrioridadeEdit  = "Media";
        EditandoPendencia   = true;
    }

    [RelayCommand]
    private async Task SalvarPendencia()
    {
        if (ProjetoSelecionado == null || string.IsNullOrWhiteSpace(PendDescricaoEdit)) return;
        var nova = new Pendencia
        {
            ProjetoId   = ProjetoSelecionado.Id,
            AtaId       = AtaSelecionada?.Id,
            Descricao   = PendDescricaoEdit,
            Responsavel = PendResponsavelEdit,
            Prazo       = PendPrazoEdit,
            Prioridade  = PendPrioridadeEdit,
            Status      = "Aberta"
        };
        var (ok, msg, id) = await _pendSvc.CriarAsync(nova);
        Mensagem = msg;
        if (ok)
        {
            nova.Id = id;
            Pendencias.Insert(0, nova);
            AplicarFiltro();
        }
        EditandoPendencia = false;
    }

    [RelayCommand] private void CancelarPendencia() => EditandoPendencia = false;

    [RelayCommand]
    private async Task AlterarStatusPendencia((Pendencia p, string novoStatus) args)
    {
        await _pendSvc.AtualizarStatusAsync(args.p.Id, args.novoStatus);
        args.p.Status = args.novoStatus;
        // Força refresh da lista filtrada
        AplicarFiltro();
        OnPropertyChanged(nameof(PendenciasFiltradas));
    }

    [RelayCommand]
    private async Task ExcluirPendencia(Pendencia p)
    {
        if (p == null) return;
        await _pendSvc.ExcluirAsync(p.Id);
        Pendencias.Remove(p);
        AplicarFiltro();
    }

    [RelayCommand]
    private async Task MarcarConcluida(Pendencia p)
    {
        if (p == null) return;
        var novoStatus = p.Status == "Concluída" ? "Aberta" : "Concluída";
        await _pendSvc.AtualizarStatusAsync(p.Id, novoStatus);
        p.Status = novoStatus;
        AplicarFiltro();
    }

    private void MostrarMensagemTemporaria(string msg)
    {
        Mensagem = msg;
        Task.Delay(3000).ContinueWith(_ => Mensagem = null);
    }
}
