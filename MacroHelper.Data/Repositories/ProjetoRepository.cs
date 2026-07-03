using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class ProjetoRepository
{
    private readonly SupabaseContext _ctx;
    public ProjetoRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Projeto>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<ProjetosModel>()
            .Order("criado_em", Ordering.Descending)
            .Get();
        return resp.Models.Select(Map);
    }

    public async Task<Projeto?> GetByIdAsync(int id)
    {
        var m = await _ctx.Client.From<ProjetosModel>()
            .Filter("id", Operator.Equals, id.ToString())
            .Single();
        return m == null ? null : Map(m);
    }

    public async Task<int> InsertAsync(Projeto p)
    {
        var model = ToModel(p);
        model.CriadoEm = DateTime.Now;
        var r = await _ctx.Client.From<ProjetosModel>().Insert(model);
        return r.Models.First().Id;
    }

    public async Task UpdateAsync(Projeto p)
    {
        var m = await _ctx.Client.From<ProjetosModel>()
            .Filter("id", Operator.Equals, p.Id.ToString()).Single();
        if (m == null) return;
        m.Nome = p.Nome; m.Descricao = p.Descricao; m.Status = p.Status;
        m.DataInicio = p.DataInicio; m.DataFimPrevista = p.DataFimPrevista;
        await _ctx.Client.From<ProjetosModel>().Update(m);
    }

    public async Task DeleteAsync(int id) =>
        await _ctx.Client.From<ProjetosModel>().Filter("id", Operator.Equals, id.ToString()).Delete();

    private static Projeto Map(ProjetosModel m) => new()
    {
        Id = m.Id, Nome = m.Nome, Descricao = m.Descricao, Status = m.Status,
        DataInicio = m.DataInicio, DataFimPrevista = m.DataFimPrevista, CriadoEm = m.CriadoEm
    };

    private static ProjetosModel ToModel(Projeto p) => new()
    {
        Id = p.Id, Nome = p.Nome, Descricao = p.Descricao, Status = p.Status,
        DataInicio = p.DataInicio, DataFimPrevista = p.DataFimPrevista, CriadoEm = p.CriadoEm
    };
}
