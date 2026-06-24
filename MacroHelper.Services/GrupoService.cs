using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class GrupoService
{
    private readonly GrupoRepository _repo;
    public GrupoService(GrupoRepository repo) => _repo = repo;

    public async Task<IEnumerable<Grupo>> ObterTodosAsync() => await _repo.GetAllAsync();

    public async Task<(bool Ok, string Msg)> CriarAsync(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return (false, "Nome é obrigatório.");
        await _repo.InsertAsync(nome.Trim());
        return (true, "Grupo criado!");
    }

    public async Task<(bool Ok, string Msg)> EditarAsync(int id, string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return (false, "Nome é obrigatório.");
        await _repo.UpdateAsync(id, nome.Trim());
        return (true, "Grupo atualizado!");
    }

    public async Task ExcluirAsync(int id) => await _repo.DeleteAsync(id);

    public async Task AtribuirUsuarioAsync(int usuarioId, int? grupoId) =>
        await _repo.AtualizarGrupoUsuarioAsync(usuarioId, grupoId);

    public async Task<IEnumerable<Usuario>> ObterMembrosAsync(int grupoId) =>
        await _repo.GetUsuariosDoGrupoAsync(grupoId);
}
