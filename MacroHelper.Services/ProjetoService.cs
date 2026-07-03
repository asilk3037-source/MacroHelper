using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class ProjetoService
{
    private readonly ProjetoRepository _repo;
    public ProjetoService(ProjetoRepository repo) => _repo = repo;

    public Task<IEnumerable<Projeto>> ObterTodosAsync() => _repo.GetAllAsync();
    public Task<Projeto?> ObterPorIdAsync(int id)       => _repo.GetByIdAsync(id);

    public async Task<(bool Ok, string Msg, int Id)> CriarAsync(Projeto p)
    {
        if (string.IsNullOrWhiteSpace(p.Nome)) return (false, "Nome é obrigatório.", 0);
        var id = await _repo.InsertAsync(p);
        return (true, "Projeto criado!", id);
    }

    public async Task<(bool Ok, string Msg)> SalvarAsync(Projeto p)
    {
        if (string.IsNullOrWhiteSpace(p.Nome)) return (false, "Nome é obrigatório.");
        await _repo.UpdateAsync(p);
        return (true, "Projeto salvo!");
    }

    public Task ExcluirAsync(int id) => _repo.DeleteAsync(id);
}
