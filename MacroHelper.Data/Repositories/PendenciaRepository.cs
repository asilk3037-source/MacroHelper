using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class PendenciaRepository
{
    private readonly SupabaseContext _ctx;
    public PendenciaRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Pendencia>> GetByProjetoAsync(int projetoId)
    {
        var resp = await _ctx.Client.From<PendenciasModel>()
            .Filter("projeto_id", Operator.Equals, projetoId.ToString())
            .Order("criado_em", Ordering.Ascending)
            .Get();
        return resp.Models.Select(Map);
    }

    public async Task<IEnumerable<Pendencia>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<PendenciasModel>()
            .Order("projeto_id", Ordering.Ascending)
            .Order("criado_em", Ordering.Ascending)
            .Get();
        return resp.Models.Select(Map);
    }

    public async Task<Pendencia?> GetByIdAsync(int id)
    {
        var m = await _ctx.Client.From<PendenciasModel>()
            .Filter("id", Operator.Equals, id.ToString())
            .Single();
        return m == null ? null : Map(m);
    }

    public async Task<int> InsertAsync(Pendencia p)
    {
        var model = ToModel(p);
        model.CriadoEm = DateTime.Now;
        model.AtualizadoEm = DateTime.Now;
        var r = await _ctx.Client.From<PendenciasModel>().Insert(model);
        return r.Models.First().Id;
    }

    public async Task UpdateAsync(Pendencia p)
    {
        var m = await _ctx.Client.From<PendenciasModel>()
            .Filter("id", Operator.Equals, p.Id.ToString()).Single();
        if (m == null) return;
        m.Descricao = p.Descricao; m.Responsavel = p.Responsavel; m.Prazo = p.Prazo;
        m.Status = p.Status; m.Prioridade = p.Prioridade; m.AtualizadoEm = DateTime.Now;
        await _ctx.Client.From<PendenciasModel>().Update(m);
    }

    public async Task AtualizarStatusAsync(int id, string status)
    {
        var m = await _ctx.Client.From<PendenciasModel>()
            .Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.Status = status; m.AtualizadoEm = DateTime.Now;
        await _ctx.Client.From<PendenciasModel>().Update(m);
    }

    public async Task DeleteAsync(int id) =>
        await _ctx.Client.From<PendenciasModel>().Filter("id", Operator.Equals, id.ToString()).Delete();

    private static Pendencia Map(PendenciasModel m) => new()
    {
        Id = m.Id, ProjetoId = m.ProjetoId, AtaId = m.AtaId, Descricao = m.Descricao,
        Responsavel = m.Responsavel, Prazo = m.Prazo, Status = m.Status, Prioridade = m.Prioridade,
        CriadoEm = m.CriadoEm, AtualizadoEm = m.AtualizadoEm
    };

    private static PendenciasModel ToModel(Pendencia p) => new()
    {
        Id = p.Id, ProjetoId = p.ProjetoId, AtaId = p.AtaId, Descricao = p.Descricao,
        Responsavel = p.Responsavel, Prazo = p.Prazo, Status = p.Status, Prioridade = p.Prioridade,
        CriadoEm = p.CriadoEm, AtualizadoEm = p.AtualizadoEm
    };
}
