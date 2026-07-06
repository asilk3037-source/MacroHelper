using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class IntimacaoErroRepository
{
    private readonly SupabaseContext _ctx;

    public IntimacaoErroRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<List<IntimacaoErro>> GetAllAsync()
    {
        var r = await _ctx.Client.From<IntimacaoErroModel>()
            .Order("data_email", Ordering.Descending)
            .Order("id", Ordering.Descending)
            .Get();
        return r.Models.Select(Map).ToList();
    }

    public async Task<List<IntimacaoErro>> GetAbertosByAdvSistemaAsync(string advogado, string sistema)
    {
        var r = await _ctx.Client.From<IntimacaoErroModel>()
            .Filter("advogado", Operator.Equals, advogado)
            .Filter("sistema", Operator.Equals, sistema)
            .Filter("status", Operator.NotEqual, "Resolvido")
            .Get();
        return r.Models.Select(Map).ToList();
    }

    public async Task<IntimacaoErro> InsertAsync(IntimacaoErro e)
    {
        var m = ToModel(e);
        var r = await _ctx.Client.From<IntimacaoErroModel>().Insert(m);
        return Map(r.Models.First());
    }

    public async Task UpdateAsync(IntimacaoErro e)
    {
        var m = ToModel(e);
        m.AtualizadoEm = DateTime.UtcNow;
        await _ctx.Client.From<IntimacaoErroModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        await _ctx.Client.From<IntimacaoErroModel>()
            .Filter("id", Operator.Equals, id.ToString())
            .Delete();
    }

    private static IntimacaoErro Map(IntimacaoErroModel m) => new()
    {
        Id           = (int)m.Id,
        DataEmail    = m.DataEmail,
        Advogado     = m.Advogado,
        TipoLogin    = m.TipoLogin,
        Sistema      = m.Sistema,
        LinkTribunal = m.LinkTribunal,
        Erro         = m.Erro,
        Status       = m.Status,
        Observacao   = m.Observacao,
        CriadoEm    = m.CriadoEm,
        AtualizadoEm = m.AtualizadoEm,
        ResolvidoEm  = m.ResolvidoEm,
    };

    private static IntimacaoErroModel ToModel(IntimacaoErro e) => new()
    {
        Id           = e.Id,
        DataEmail    = e.DataEmail,
        Advogado     = e.Advogado,
        TipoLogin    = e.TipoLogin,
        Sistema      = e.Sistema,
        LinkTribunal = e.LinkTribunal,
        Erro         = e.Erro,
        Status       = e.Status,
        Observacao   = e.Observacao,
        CriadoEm    = e.CriadoEm.ToUniversalTime(),
        AtualizadoEm = e.AtualizadoEm.ToUniversalTime(),
        ResolvidoEm  = e.ResolvidoEm?.ToUniversalTime(),
    };
}
