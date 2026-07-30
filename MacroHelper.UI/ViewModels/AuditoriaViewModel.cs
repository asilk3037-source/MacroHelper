using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Data.Repositories;
using System.Collections.ObjectModel;
using System.IO;

namespace MacroHelper.UI.ViewModels;

public partial class AuditoriaViewModel : ObservableObject
{
    private readonly LogAuditoriaRepository _repo;
    private CancellationTokenSource? _buscaDebounceCts;

    [ObservableProperty] private ObservableCollection<AuditoriaItem> _registros = new();
    [ObservableProperty] private string _busca = string.Empty;
    [ObservableProperty] private bool   _isLoading = false;
    [ObservableProperty] private string? _mensagem;

    private CancellationTokenSource? _msgCts;

    public AuditoriaViewModel(LogAuditoriaRepository repo) => _repo = repo;

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = (await _repo.GetRecentesComNomeAsync(300))
                .Select(a => new AuditoriaItem(a.Data, a.Acao, a.Entidade, a.Detalhes, a.UsuarioNome));

            if (!string.IsNullOrWhiteSpace(Busca))
                lista = lista.Where(a =>
                    a.Acao.Contains(Busca, StringComparison.OrdinalIgnoreCase) ||
                    a.Entidade.Contains(Busca, StringComparison.OrdinalIgnoreCase) ||
                    a.UsuarioNome.Contains(Busca, StringComparison.OrdinalIgnoreCase) ||
                    (a.Detalhes ?? "").Contains(Busca, StringComparison.OrdinalIgnoreCase));

            Registros = new ObservableCollection<AuditoriaItem>(lista);
        }
        finally { IsLoading = false; }
    }

    partial void OnBuscaChanged(string value) => _ = DebounceCarregarAsync();

    /// <summary>Espera o usuário parar de digitar (300ms) antes de filtrar — evita uma chamada ao banco por tecla.</summary>
    private async Task DebounceCarregarAsync()
    {
        _buscaDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _buscaDebounceCts = cts;
        try { await Task.Delay(300, cts.Token); }
        catch (TaskCanceledException) { return; }
        if (cts.IsCancellationRequested) return;
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task ExportarCsvAsync()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"MacroHelper_Auditoria_{DateTime.Now:yyyyMMdd_HHmm}.csv");

            var linhas = new List<string> { "Data,Hora,Usuario,Acao,Entidade,Detalhes" };
            foreach (var r in Registros)
                linhas.Add($"{r.Data:dd/MM/yyyy},{r.Data:HH:mm:ss},\"{r.UsuarioNome}\",\"{r.Acao}\",\"{r.Entidade}\",\"{(r.Detalhes ?? "").Replace("\"", "'")}\"");

            await File.WriteAllLinesAsync(path, linhas, System.Text.Encoding.UTF8);
            Mensagem = "Exportado para a Área de Trabalho!";
            _msgCts?.Cancel();
            var cts = _msgCts = new CancellationTokenSource();
            if (System.Threading.SynchronizationContext.Current != null)
                Task.Delay(4000, cts.Token).ContinueWith(_ => Mensagem = null,
                    CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch (Exception ex) { Mensagem = $"Erro: {ex.Message}"; }
    }
}
