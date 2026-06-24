using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class CategoriaService
{
    private readonly CategoriaRepository _repo;
    public CategoriaService(CategoriaRepository repo) => _repo = repo;

    public async Task<IEnumerable<Categoria>> ObterTodosAsync() => await _repo.GetAllAsync();
    public async Task<IEnumerable<Categoria>> ObterRaizAsync()   => await _repo.GetRaizAsync();

    public async Task<IEnumerable<Categoria>> ObterArvoreAsync()
    {
        var todos = (await _repo.GetAllAsync()).ToList();
        var raiz  = todos.Where(c => c.PaiId == null).ToList();
        foreach (var cat in raiz)
            cat.Subcategorias = todos.Where(c => c.PaiId == cat.Id).ToList();
        return raiz;
    }

    public async Task<(bool Ok, string Msg)> SalvarAsync(Categoria cat)
    {
        if (string.IsNullOrWhiteSpace(cat.Nome)) return (false, "Nome é obrigatório.");
        if (cat.Id == 0) await _repo.InsertAsync(cat);
        else             await _repo.UpdateAsync(cat);
        return (true, "Categoria salva!");
    }

    public async Task<(bool Ok, string Msg)> ExcluirAsync(int id)
    {
        await _repo.DeleteAsync(id);
        return (true, "Categoria excluída.");
    }
}
