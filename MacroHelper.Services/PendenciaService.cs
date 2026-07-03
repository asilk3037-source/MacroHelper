using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class PendenciaService
{
    private readonly PendenciaRepository _repo;
    public PendenciaService(PendenciaRepository repo) => _repo = repo;

    public Task<IEnumerable<Pendencia>> ObterPorProjetoAsync(int projetoId) => _repo.GetByProjetoAsync(projetoId);
    public Task<IEnumerable<Pendencia>> ObterTodasAsync()                    => _repo.GetAllAsync();
    public Task<Pendencia?> ObterPorIdAsync(int id)                          => _repo.GetByIdAsync(id);

    public async Task<(bool Ok, string Msg, int Id)> CriarAsync(Pendencia p)
    {
        if (string.IsNullOrWhiteSpace(p.Descricao)) return (false, "Descrição é obrigatória.", 0);
        var id = await _repo.InsertAsync(p);
        return (true, "Pendência criada!", id);
    }

    public async Task<(bool Ok, string Msg)> SalvarAsync(Pendencia p)
    {
        if (string.IsNullOrWhiteSpace(p.Descricao)) return (false, "Descrição é obrigatória.");
        await _repo.UpdateAsync(p);
        return (true, "Pendência salva!");
    }

    public Task AtualizarStatusAsync(int id, string status) => _repo.AtualizarStatusAsync(id, status);

    public Task ExcluirAsync(int id) => _repo.DeleteAsync(id);
}
