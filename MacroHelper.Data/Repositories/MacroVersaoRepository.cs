using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class MacroVersaoRepository
{
    private readonly SupabaseContext _ctx;
    public MacroVersaoRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task RegistrarAsync(int macroId, string titulo, string conteudo, int? usuarioId)
    {
        await _ctx.Client.From<MacroVersoesModel>().Insert(new MacroVersoesModel
        {
            MacroId = macroId, Titulo = titulo, Conteudo = conteudo,
            ModificadoPorId = usuarioId, DataModificacao = DateTime.Now
        });
    }

    public async Task<IEnumerable<MacroVersao>> GetByMacroAsync(int macroId)
    {
        var resp = await _ctx.Client.From<MacroVersoesModel>()
            .Filter("macro_id", Operator.Equals, macroId.ToString())
            .Order("data_modificacao", Ordering.Descending)
            .Get();
        return resp.Models.Select(m => new MacroVersao
        {
            Id = m.Id, MacroId = m.MacroId, Titulo = m.Titulo, Conteudo = m.Conteudo,
            ModificadoPorId = m.ModificadoPorId, DataModificacao = m.DataModificacao
        });
    }
}
