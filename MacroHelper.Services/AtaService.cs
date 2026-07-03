using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class AtaService
{
    private readonly AtaRepository _repo;
    public AtaService(AtaRepository repo) => _repo = repo;

    public Task<IEnumerable<Ata>> ObterPorProjetoAsync(int projetoId) => _repo.GetByProjetoAsync(projetoId);
    public Task<Ata?> ObterPorIdAsync(int id)                          => _repo.GetByIdAsync(id);

    public async Task<(bool Ok, string Msg, int Id)> CriarAsync(Ata a)
    {
        if (string.IsNullOrWhiteSpace(a.Titulo)) return (false, "Título é obrigatório.", 0);
        var id = await _repo.InsertAsync(a);
        return (true, "Ata criada!", id);
    }

    public async Task<(bool Ok, string Msg)> SalvarAsync(Ata a)
    {
        if (string.IsNullOrWhiteSpace(a.Titulo)) return (false, "Título é obrigatório.");
        await _repo.UpdateAsync(a);
        return (true, "Ata salva!");
    }

    public Task ExcluirAsync(int id) => _repo.DeleteAsync(id);
}
