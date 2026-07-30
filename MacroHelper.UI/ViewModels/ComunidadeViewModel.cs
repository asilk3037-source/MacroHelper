using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class ComunidadeViewModel : ObservableObject
{
    private readonly MacroService _svc;

    [ObservableProperty] private ObservableCollection<Macro> _macrosPublicas = new();
    [ObservableProperty] private bool    _isLoading = false;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool    _mensagemSucesso = true;

    private CancellationTokenSource? _msgCts;

    public ComunidadeViewModel(MacroService svc) => _svc = svc;

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try { MacrosPublicas = new ObservableCollection<Macro>(await _svc.ObterPublicasAsync()); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task CurtirAsync(Macro macro)
    {
        await _svc.ToggleCurtidaAsync(macro.Id);
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task CopiarParaMinhasAsync(Macro macro)
    {
        var (ok, msg) = await _svc.CopiarParaMinhasAsync(macro.Id);
        MostrarMensagem(msg, ok);
    }

    private void MostrarMensagem(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        _msgCts?.Cancel();
        var cts = _msgCts = new CancellationTokenSource();
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500, cts.Token).ContinueWith(_ => Mensagem = null,
                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}
