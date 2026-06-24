using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class IntegracoesViewModel : ObservableObject
{
    private readonly WebhookService _svc;

    [ObservableProperty] private ObservableCollection<Webhook> _webhooks = new();
    [ObservableProperty] private bool    _mostrarFormulario = false;
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;

    [ObservableProperty] private int     _formId = 0;
    [ObservableProperty] private string  _formNome = string.Empty;
    [ObservableProperty] private string  _formUrl = string.Empty;
    [ObservableProperty] private string  _formEvento = EventosWebhook.MacroUsada;
    [ObservableProperty] private bool    _formAtivo = true;
    [ObservableProperty] private string? _formErro;

    public string[] EventosDisponiveis => EventosWebhook.Todos;

    public IntegracoesViewModel(WebhookService svc) => _svc = svc;

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try { Webhooks = new ObservableCollection<Webhook>(await _svc.ObterTodosAsync()); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public void NovoWebhook()
    {
        FormId = 0; FormNome = string.Empty; FormUrl = string.Empty;
        FormEvento = EventosWebhook.MacroUsada; FormAtivo = true; FormErro = null;
        MostrarFormulario = true;
    }

    [RelayCommand]
    public void EditarWebhook(Webhook w)
    {
        FormId = w.Id; FormNome = w.Nome; FormUrl = w.Url; FormEvento = w.Evento; FormAtivo = w.Ativo;
        FormErro = null; MostrarFormulario = true;
    }

    [RelayCommand]
    public void CancelarForm() => MostrarFormulario = false;

    [RelayCommand]
    public async Task SalvarWebhookAsync()
    {
        FormErro = null;
        var w = new Webhook { Id = FormId, Nome = FormNome, Url = FormUrl, Evento = FormEvento, Ativo = FormAtivo };
        var (ok, msg) = await _svc.SalvarAsync(w);
        if (!ok) { FormErro = msg; return; }
        MostrarFormulario = false;
        await CarregarAsync();
        MostrarMensagem(msg, true);
    }

    [RelayCommand]
    public async Task ExcluirWebhookAsync(Webhook w)
    {
        await _svc.ExcluirAsync(w.Id);
        await CarregarAsync();
        MostrarMensagem("Webhook excluído.", true);
    }

    [RelayCommand]
    public async Task TestarWebhookAsync(Webhook w)
    {
        var (ok, msg) = await _svc.TestarAsync(w);
        MostrarMensagem(msg, ok);
    }

    private void MostrarMensagem(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => Mensagem = null, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
