using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class NotificacoesViewModel : ObservableObject
{
    private readonly NotificacaoService _svc;

    [ObservableProperty] private ObservableCollection<Notificacao> _notificacoes = new();
    [ObservableProperty] private bool _isLoading = false;

    public NotificacoesViewModel(NotificacaoService svc) => _svc = svc;

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try { Notificacoes = new ObservableCollection<Notificacao>(await _svc.ObterRecentesAsync()); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task MarcarComoLidaAsync(Notificacao n)
    {
        await _svc.MarcarComoLidaAsync(n.Id);
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task MarcarTodasComoLidasAsync()
    {
        await _svc.MarcarTodasComoLidasAsync();
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task LimparAsync()
    {
        await _svc.LimparAsync();
        await CarregarAsync();
    }
}
