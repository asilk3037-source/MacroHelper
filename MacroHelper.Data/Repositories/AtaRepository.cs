using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class AtaRepository
{
    private readonly SupabaseContext _ctx;
    public AtaRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Ata>> GetByProjetoAsync(int projetoId)
    {
        var resp = await _ctx.Client.From<AtasModel>()
            .Filter("projeto_id", Operator.Equals, projetoId.ToString())
            .Order("data_reuniao", Ordering.Descending)
            .Get();
        return resp.Models.Select(Map);
    }

    public async Task<Ata?> GetByIdAsync(int id)
    {
        var m = await _ctx.Client.From<AtasModel>()
            .Filter("id", Operator.Equals, id.ToString())
            .Single();
        return m == null ? null : Map(m);
    }

    public async Task<int> InsertAsync(Ata a)
    {
        var model = ToModel(a);
        model.CriadoEm = DateTime.Now;
        var r = await _ctx.Client.From<AtasModel>().Insert(model);
        return r.Models.First().Id;
    }

    public async Task UpdateAsync(Ata a)
    {
        var m = await _ctx.Client.From<AtasModel>()
            .Filter("id", Operator.Equals, a.Id.ToString()).Single();
        if (m == null) return;
        m.Titulo = a.Titulo; m.Cliente = a.Cliente; m.DataReuniao = a.DataReuniao;
        m.Horario = a.Horario; m.ParticipantesNetview = a.ParticipantesNetview;
        m.ParticipantesCliente = a.ParticipantesCliente; m.Notas = a.Notas;
        await _ctx.Client.From<AtasModel>().Update(m);
    }

    public async Task DeleteAsync(int id) =>
        await _ctx.Client.From<AtasModel>().Filter("id", Operator.Equals, id.ToString()).Delete();

    private static Ata Map(AtasModel m) => new()
    {
        Id = m.Id, ProjetoId = m.ProjetoId, Titulo = m.Titulo, Cliente = m.Cliente,
        DataReuniao = m.DataReuniao, Horario = m.Horario,
        ParticipantesNetview = m.ParticipantesNetview, ParticipantesCliente = m.ParticipantesCliente,
        Notas = m.Notas, CriadoEm = m.CriadoEm
    };

    private static AtasModel ToModel(Ata a) => new()
    {
        Id = a.Id, ProjetoId = a.ProjetoId, Titulo = a.Titulo, Cliente = a.Cliente,
        DataReuniao = a.DataReuniao, Horario = a.Horario,
        ParticipantesNetview = a.ParticipantesNetview, ParticipantesCliente = a.ParticipantesCliente,
        Notas = a.Notas, CriadoEm = a.CriadoEm
    };
}
