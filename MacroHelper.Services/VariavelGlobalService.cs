using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;
using System.Text.RegularExpressions;

namespace MacroHelper.Services;

public class VariavelGlobalService
{
    private readonly VariavelGlobalRepository _repo;
    private static readonly Regex _regex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    public VariavelGlobalService(VariavelGlobalRepository repo) => _repo = repo;

    public async Task<IEnumerable<VariavelGlobal>> ObterTodasAsync() => await _repo.GetAllAsync();

    public async Task<(bool Ok, string Msg)> SalvarAsync(VariavelGlobal v)
    {
        if (string.IsNullOrWhiteSpace(v.Nome)) return (false, "Nome é obrigatório.");
        v.Nome = v.Nome.Trim().ToLowerInvariant();

        if (v.Id == 0) await _repo.InsertAsync(v);
        else           await _repo.UpdateAsync(v);
        return (true, "Variável global salva!");
    }

    public async Task ExcluirAsync(int id) => await _repo.DeleteAsync(id);

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
