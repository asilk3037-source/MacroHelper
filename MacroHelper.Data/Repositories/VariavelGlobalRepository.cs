using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class VariavelGlobalRepository
{
    private readonly SupabaseContext _ctx;
    public VariavelGlobalRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<VariavelGlobal>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<VariaveisGlobaisModel>().Order("nome", Ordering.Ascending).Get();
        return resp.Models.Select(MapToVariavel);
    }

    public async Task<int> InsertAsync(VariavelGlobal v)
    {
        var inserted = await _ctx.Client.From<VariaveisGlobaisModel>().Insert(new VariaveisGlobaisModel
        {
            Nome = v.Nome, ValorPadrao = v.ValorPadrao, Descricao = v.Descricao, DataCriacao = DateTime.Now
        });
        return inserted.Models.First().Id;
    }

    public async Task UpdateAsync(VariavelGlobal v)
    {
        var m = await _ctx.Client.From<VariaveisGlobaisModel>().Filter("id", Operator.Equals, v.Id.ToString()).Single();
        if (m == null) return;
        m.Nome = v.Nome; m.ValorPadrao = v.ValorPadrao; m.Descricao = v.Descricao;
        await _ctx.Client.From<VariaveisGlobaisModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        await _ctx.Client.From<VariaveisGlobaisModel>().Filter("id", Operator.Equals, id.ToString()).Delete();
    }

    private static VariavelGlobal MapToVariavel(VariaveisGlobaisModel m) => new()
    {
        Id = m.Id, Nome = m.Nome, ValorPadrao = m.ValorPadrao, Descricao = m.Descricao, DataCriacao = m.DataCriacao
    };
}
