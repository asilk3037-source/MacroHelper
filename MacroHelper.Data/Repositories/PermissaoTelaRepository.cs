using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class PermissaoTelaRepository
{
    private readonly SupabaseContext _ctx;
    public PermissaoTelaRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<PermissaoTela>> ObterPorUsuarioAsync(int usuarioId)
    {
        var resp = await _ctx.Client.From<PermissoesTelaModel>()
            .Filter("usuario_id", Operator.Equals, usuarioId.ToString())
            .Get();
        return resp.Models.Select(MapToPermissao);
    }

    public async Task DefinirAsync(int usuarioId, string tela, string nivel)
    {
        var existente = await _ctx.Client.From<PermissoesTelaModel>()
            .Filter("usuario_id", Operator.Equals, usuarioId.ToString())
            .Filter("tela", Operator.Equals, tela)
            .Single();

        if (existente == null)
        {
            await _ctx.Client.From<PermissoesTelaModel>().Insert(new PermissoesTelaModel
            {
                UsuarioId = usuarioId, Tela = tela, Nivel = nivel, DataAtualizacao = DateTime.Now
            });
        }
        else
        {
            existente.Nivel = nivel;
            existente.DataAtualizacao = DateTime.Now;
            await _ctx.Client.From<PermissoesTelaModel>().Update(existente);
        }
    }

    public async Task RemoverTodasDoUsuarioAsync(int usuarioId)
    {
        await _ctx.Client.From<PermissoesTelaModel>()
            .Filter("usuario_id", Operator.Equals, usuarioId.ToString())
            .Delete();
    }

    private static PermissaoTela MapToPermissao(PermissoesTelaModel m) => new()
    {
        Id = m.Id, UsuarioId = m.UsuarioId, Tela = m.Tela, Nivel = m.Nivel
    };
}
