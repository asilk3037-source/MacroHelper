using CommunityToolkit.Mvvm.ComponentModel;
using MacroHelper.Data.Repositories;
using System.Collections.ObjectModel;

namespace MacroHelper.UI.ViewModels;

public partial class AuditoriaViewModel : ObservableObject
{
    private readonly LogAuditoriaRepository _repo;

    [ObservableProperty] private ObservableCollection<AuditoriaItem> _registros = new();
    [ObservableProperty] private string _busca = string.Empty;
    [ObservableProperty] private bool   _isLoading = false;

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

    partial void OnBuscaChanged(string value) => _ = CarregarAsync();
}
