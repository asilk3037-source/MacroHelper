using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class CategoriaRepository
{
    private readonly SupabaseContext _ctx;
    public CategoriaRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Categoria>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<CategoriasModel>().Get();
        var todas = resp.Models.ToList();
        var nomesPorId = todas.ToDictionary(c => c.Id, c => c.Nome);

        return todas
            .OrderBy(c => c.PaiId.HasValue) // PaiId NULL primeiro, igual "NULLS FIRST"
            .ThenBy(c => c.Ordem)
            .ThenBy(c => c.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(c => MapToCategoria(c, c.PaiId.HasValue && nomesPorId.TryGetValue(c.PaiId.Value, out var nomePai) ? nomePai : null));
    }

    public async Task<IEnumerable<Categoria>> GetRaizAsync()
    {
        var resp = await _ctx.Client.From<CategoriasModel>()
            .Filter("pai_id", Operator.Is, "null")
            .Order("ordem", Ordering.Ascending)
            .Order("nome", Ordering.Ascending)
            .Get();
        return resp.Models.Select(c => MapToCategoria(c, null));
    }

    public async Task<IEnumerable<Categoria>> GetSubcategoriasAsync(int paiId)
    {
        var resp = await _ctx.Client.From<CategoriasModel>()
            .Filter("pai_id", Operator.Equals, paiId.ToString())
            .Order("ordem", Ordering.Ascending)
            .Order("nome", Ordering.Ascending)
            .Get();
        return resp.Models.Select(c => MapToCategoria(c, null));
    }

    public async Task<int> InsertAsync(Categoria cat)
    {
        var inserted = await _ctx.Client.From<CategoriasModel>().Insert(new CategoriasModel
        {
            Nome = cat.Nome, Icone = cat.Icone, Cor = cat.Cor, PaiId = cat.PaiId,
            Ordem = cat.Ordem, DataCriacao = DateTime.Now
        });
        return inserted.Models.First().Id;
    }

    public async Task UpdateAsync(Categoria cat)
    {
        var m = await _ctx.Client.From<CategoriasModel>().Filter("id", Operator.Equals, cat.Id.ToString()).Single();
        if (m == null) return;
        m.Nome = cat.Nome; m.Icone = cat.Icone; m.Cor = cat.Cor; m.PaiId = cat.PaiId; m.Ordem = cat.Ordem;
        await _ctx.Client.From<CategoriasModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        var orfas = await _ctx.Client.From<CategoriasModel>().Filter("pai_id", Operator.Equals, id.ToString()).Get();
        foreach (var orfa in orfas.Models)
        {
            orfa.PaiId = null;
            await _ctx.Client.From<CategoriasModel>().Update(orfa);
        }
        await _ctx.Client.From<CategoriasModel>().Filter("id", Operator.Equals, id.ToString()).Delete();
    }

    private static Categoria MapToCategoria(CategoriasModel m, string? nomePai) => new()
    {
        Id = m.Id, Nome = m.Nome, Icone = m.Icone, Cor = m.Cor, PaiId = m.PaiId,
        NomePai = nomePai, Ordem = m.Ordem, DataCriacao = m.DataCriacao
    };
}
