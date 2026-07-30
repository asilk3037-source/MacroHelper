using MacroHelper.Core.Entities;

namespace MacroHelper.Core.Interfaces;

public interface IMacroRepository
{
    Task<IEnumerable<Macro>> GetAllAsync();
    Task<IEnumerable<Macro>> SearchAsync(string termo);
    Task<IEnumerable<Macro>> GetByCategoriaAsync(string categoria);
    Task<Macro?> GetByIdAsync(int id);
    Task<Macro?> GetByAtalhoAsync(string atalho);
    Task<int> InsertAsync(Macro macro);
    Task BatchInsertAsync(IEnumerable<Macro> macros);
    Task UpdateAsync(Macro macro);
    Task DeleteAsync(int id);
    Task<IEnumerable<string>> GetCategoriasAsync();
    Task<Macro?> GetByAtalhoTeclaAsync(string digito);
    Task<IEnumerable<Macro>> GetFavoritosAsync();
    Task<IEnumerable<Macro>> GetPendentesAsync();
    Task ToggleFavoritoAsync(int id, bool favorito);
    Task ToggleAtivoAsync(int id, bool ativo);
    Task AtualizarStatusAsync(int id, string status);

    // Comunidade interna
    Task<IEnumerable<Macro>> GetPublicasAsync();
    Task TogglePublicaAsync(int id, bool publica);
    Task<bool> ToggleCurtidaAsync(int macroId, int usuarioId);
    Task<HashSet<int>> ObterCurtidasDoUsuarioAsync(int usuarioId);
}
