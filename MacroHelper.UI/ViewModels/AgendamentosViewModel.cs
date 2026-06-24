using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class AgendamentosViewModel : ObservableObject
{
    private readonly AgendamentoService _svc;
    private readonly MacroService       _macroService;

    [ObservableProperty] private ObservableCollection<MacroAgendamento> _agendamentos = new();
    [ObservableProperty] private ObservableCollection<Macro> _macrosDisponiveis = new();
    [ObservableProperty] private bool    _mostrarFormulario = false;
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;

    [ObservableProperty] private int     _formId = 0;
    [ObservableProperty] private Macro?  _formMacro;
    [ObservableProperty] private string  _formHorario = "09:00";
    [ObservableProperty] private bool    _formAtivo = true;
    [ObservableProperty] private string? _formErro;

    public ObservableCollection<DiaSemanaOpcao> DiasDisponiveis { get; } = new(
    [
        new(1, "Seg"), new(2, "Ter"), new(3, "Qua"), new(4, "Qui"),
        new(5, "Sex"), new(6, "Sáb"), new(0, "Dom")
    ]);

    public AgendamentosViewModel(AgendamentoService svc, MacroService macroService)
    {
        _svc = svc;
        _macroService = macroService;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            Agendamentos = new ObservableCollection<MacroAgendamento>(await _svc.ObterTodosAsync());
            MacrosDisponiveis = new ObservableCollection<Macro>(await _macroService.ObterTodosAsync());
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public void NovoAgendamento()
    {
        FormId = 0; FormMacro = MacrosDisponiveis.FirstOrDefault(); FormHorario = "09:00"; FormAtivo = true;
        foreach (var d in DiasDisponiveis) d.Selecionado = d.Valor >= 1 && d.Valor <= 5;
        FormErro = null; MostrarFormulario = true;
    }

    [RelayCommand]
    public void EditarAgendamento(MacroAgendamento a)
    {
        FormId = a.Id; FormMacro = MacrosDisponiveis.FirstOrDefault(m => m.Id == a.MacroId);
        FormHorario = a.Horario; FormAtivo = a.Ativo;
        var diasSelecionados = a.DiasSemana.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        foreach (var d in DiasDisponiveis) d.Selecionado = diasSelecionados.Contains(d.Valor);
        FormErro = null; MostrarFormulario = true;
    }

    [RelayCommand]
    public void CancelarForm() => MostrarFormulario = false;

    [RelayCommand]
    public async Task SalvarAgendamentoAsync()
    {
        FormErro = null;
        if (FormMacro == null) { FormErro = "Selecione uma macro."; return; }

        var dias = string.Join(",", DiasDisponiveis.Where(d => d.Selecionado).Select(d => d.Valor));
        var a = new MacroAgendamento { Id = FormId, MacroId = FormMacro.Id, DiasSemana = dias, Horario = FormHorario, Ativo = FormAtivo };
        var (ok, msg) = await _svc.SalvarAsync(a);
        if (!ok) { FormErro = msg; return; }
        MostrarFormulario = false;
        await CarregarAsync();
        MostrarMensagem(msg, true);
    }

    [RelayCommand]
    public async Task ExcluirAgendamentoAsync(MacroAgendamento a)
    {
        await _svc.ExcluirAsync(a.Id);
        await CarregarAsync();
        MostrarMensagem("Agendamento excluído.", true);
    }

    private void MostrarMensagem(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => Mensagem = null, TaskScheduler.FromCurrentSynchronizationContext());
    }
}

public partial class DiaSemanaOpcao : ObservableObject
{
    public int    Valor { get; }
    public string Label { get; }
    [ObservableProperty] private bool _selecionado;

    public DiaSemanaOpcao(int valor, string label) { Valor = valor; Label = label; }
}
