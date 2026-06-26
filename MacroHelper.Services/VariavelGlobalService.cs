using MacroHelper.Core.Entities;
using MacroHelper.Core.Interfaces;
using MacroHelper.Data.Repositories;
using System.Text.RegularExpressions;

namespace MacroHelper.Services;

public class VariavelGlobalService
{
    private readonly VariavelGlobalRepository _repo;
    private readonly IMacroRepository? _macroRepo;
    private readonly UsuarioService? _usuarioService;
    private readonly LogAuditoriaRepository? _auditoriaRepo;
    private static readonly Regex _regex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    public VariavelGlobalService(VariavelGlobalRepository repo, IMacroRepository? macroRepo = null,
        UsuarioService? usuarioService = null, LogAuditoriaRepository? auditoriaRepo = null)
    {
        _repo = repo;
        _macroRepo = macroRepo;
        _usuarioService = usuarioService;
        _auditoriaRepo = auditoriaRepo;
    }

    public async Task<IEnumerable<VariavelGlobal>> ObterTodasAsync() => await _repo.GetAllAsync();

    /// <summary>Quantidade de macros cujo conteúdo referencia {nome} — para avisar antes de excluir.</summary>
    public async Task<int> ContarUsoAsync(string nome)
    {
        if (_macroRepo == null) return 0;
        var placeholder = "{" + nome + "}";
        var todos = await _macroRepo.GetAllAsync();
        return todos.Count(m => m.Conteudo.Contains(placeholder, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(bool Ok, string Msg)> SalvarAsync(VariavelGlobal v)
    {
        if (string.IsNullOrWhiteSpace(v.Nome)) return (false, "Nome é obrigatório.");
        v.Nome = v.Nome.Trim().ToLowerInvariant();

        var ehNova = v.Id == 0;
        if (ehNova) await _repo.InsertAsync(v);
        else        await _repo.UpdateAsync(v);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService?.UsuarioAtual?.Id,
                ehNova ? "Criar" : "Editar", "VariavelGlobal", v.Id, v.Nome);
        return (true, "Variável global salva!");
    }

    public async Task ExcluirAsync(int id)
    {
        await _repo.DeleteAsync(id);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService?.UsuarioAtual?.Id, "Excluir", "VariavelGlobal", id, null);
    }

    /// <summary>Substitui placeholders {nome} no conteúdo pelos valores das variáveis globais cadastradas.</summary>
    public async Task<string> ResolverAsync(string conteudo)
    {
        if (!_regex.IsMatch(conteudo)) return conteudo;
        var globais = (await _repo.GetAllAsync()).ToDictionary(g => g.Nome, g => g.ValorPadrao, StringComparer.OrdinalIgnoreCase);
        if (globais.Count == 0) return conteudo;

        return _regex.Replace(conteudo, m =>
            globais.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
    }
}
