using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class GrupoRepository
{
    private readonly SupabaseContext _ctx;
    public GrupoRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Grupo>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<GruposModel>().Order("nome", Ordering.Ascending).Get();
        return resp.Models.Select(MapToGrupo);
    }

    public async Task<int> InsertAsync(string nome)
    {
        var inserted = await _ctx.Client.From<GruposModel>().Insert(new GruposModel
        {
            Nome = nome, DataCriacao = DateTime.Now
        });
        return inserted.Models.First().Id;
    }

    public async Task UpdateAsync(int id, string nome)
    {
        var m = await _ctx.Client.From<GruposModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.Nome = nome;
        await _ctx.Client.From<GruposModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        await _ctx.Client.From<GruposModel>().Filter("id", Operator.Equals, id.ToString()).Delete();
    }

    public async Task AtualizarGrupoUsuarioAsync(int usuarioId, int? grupoId)
    {
        var m = await _ctx.Client.From<UsuariosModel>().Filter("id", Operator.Equals, usuarioId.ToString()).Single();
        if (m == null) return;
        m.GrupoId = grupoId;
        await _ctx.Client.From<UsuariosModel>().Update(m);
    }

    public async Task<IEnumerable<Usuario>> GetUsuariosDoGrupoAsync(int grupoId)
    {
        var resp = await _ctx.Client.From<UsuariosModel>()
            .Filter("grupo_id", Operator.Equals, grupoId.ToString())
            .Order("nome", Ordering.Ascending)
            .Get();
        return resp.Models.Select(m => new Usuario
        {
            Id = m.Id, Nome = m.Nome, Email = m.Email, Perfil = m.Perfil, Ativo = m.Ativo,
            DataCriacao = m.DataCriacao, CategoriasPermitidas = m.CategoriasPermitidas,
            GrupoId = m.GrupoId, PermissoesCustom = m.PermissoesCustom
        });
    }

    private static Grupo MapToGrupo(GruposModel m) => new()
    {
        Id = m.Id, Nome = m.Nome, DataCriacao = m.DataCriacao
    };
}
