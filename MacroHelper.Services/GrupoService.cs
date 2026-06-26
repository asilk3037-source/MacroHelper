using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class GrupoService
{
    private readonly GrupoRepository _repo;
    private readonly UsuarioService? _usuarioService;
    private readonly LogAuditoriaRepository? _auditoriaRepo;

    public GrupoService(GrupoRepository repo, UsuarioService? usuarioService = null, LogAuditoriaRepository? auditoriaRepo = null)
    {
        _repo = repo;
        _usuarioService = usuarioService;
        _auditoriaRepo = auditoriaRepo;
    }

    public async Task<IEnumerable<Grupo>> ObterTodosAsync() => await _repo.GetAllAsync();

    public async Task<(bool Ok, string Msg)> CriarAsync(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return (false, "Nome é obrigatório.");
        await _repo.InsertAsync(nome.Trim());
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService?.UsuarioAtual?.Id, "Criar", "Grupo", null, nome.Trim());
        return (true, "Grupo criado!");
    }

    public async Task<(bool Ok, string Msg)> EditarAsync(int id, string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return (false, "Nome é obrigatório.");
        await _repo.UpdateAsync(id, nome.Trim());
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService?.UsuarioAtual?.Id, "Editar", "Grupo", id, nome.Trim());
        return (true, "Grupo atualizado!");
    }

    public async Task ExcluirAsync(int id)
    {
        await _repo.DeleteAsync(id);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService?.UsuarioAtual?.Id, "Excluir", "Grupo", id, null);
    }

    public async Task AtribuirUsuarioAsync(int usuarioId, int? grupoId) =>
        await _repo.AtualizarGrupoUsuarioAsync(usuarioId, grupoId);

    public async Task<IEnumerable<Usuario>> ObterMembrosAsync(int grupoId) =>
        await _repo.GetUsuariosDoGrupoAsync(grupoId);
}
