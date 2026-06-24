using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class AgendamentoRepository
{
    private readonly SupabaseContext _ctx;
    public AgendamentoRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<MacroAgendamento>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<MacroAgendamentosModel>().Order("horario", Ordering.Ascending).Get();
        return await ComTituloDaMacroAsync(resp.Models);
    }

    public async Task<IEnumerable<MacroAgendamento>> GetAtivosAsync()
    {
        var resp = await _ctx.Client.From<MacroAgendamentosModel>()
            .Filter("ativo", Operator.Equals, "true")
            .Get();
        return await ComTituloDaMacroAsync(resp.Models);
    }

    public async Task<int> InsertAsync(MacroAgendamento a)
    {
        var inserted = await _ctx.Client.From<MacroAgendamentosModel>().Insert(new MacroAgendamentosModel
        {
            MacroId = a.MacroId, DiasSemana = a.DiasSemana, Horario = a.Horario, Ativo = a.Ativo,
            DataCriacao = DateTime.Now
        });
        return inserted.Models.First().Id;
    }

    public async Task UpdateAsync(MacroAgendamento a)
    {
        var m = await _ctx.Client.From<MacroAgendamentosModel>().Filter("id", Operator.Equals, a.Id.ToString()).Single();
        if (m == null) return;
        m.MacroId = a.MacroId; m.DiasSemana = a.DiasSemana; m.Horario = a.Horario; m.Ativo = a.Ativo;
        await _ctx.Client.From<MacroAgendamentosModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        await _ctx.Client.From<MacroAgendamentosModel>().Filter("id", Operator.Equals, id.ToString()).Delete();
    }

    public async Task MarcarExecutadoAsync(int id, DateTime quando)
    {
        var m = await _ctx.Client.From<MacroAgendamentosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.UltimaExecucao = quando;
        await _ctx.Client.From<MacroAgendamentosModel>().Update(m);
    }

    /// <summary>Sem JOIN fluente no Postgrest — busca os títulos das macros referenciadas em uma
    /// segunda chamada e combina em memória.</summary>
    private async Task<IEnumerable<MacroAgendamento>> ComTituloDaMacroAsync(IEnumerable<MacroAgendamentosModel> agendamentos)
    {
        var lista = agendamentos.ToList();
        var macroIds = lista.Select(a => a.MacroId).Distinct().ToList();

        var titulosPorId = new Dictionary<int, string>();
        if (macroIds.Count > 0)
        {
            var macros = await _ctx.Client.From<MacrosModel>()
                .Filter("id", Operator.In, macroIds.Select(id => (object)id).ToList())
                .Get();
            titulosPorId = macros.Models.ToDictionary(m => m.Id, m => m.Titulo);
        }

        return lista.Select(a => new MacroAgendamento
        {
            Id = a.Id, MacroId = a.MacroId,
            MacroTitulo = titulosPorId.TryGetValue(a.MacroId, out var titulo) ? titulo : null,
            DiasSemana = a.DiasSemana, Horario = a.Horario, Ativo = a.Ativo,
            UltimaExecucao = a.UltimaExecucao, DataCriacao = a.DataCriacao
        });
    }
}
