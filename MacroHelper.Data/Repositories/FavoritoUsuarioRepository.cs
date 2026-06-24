using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

/// <summary>Favoritos pessoais por usuário — separados do "destaque" global em Macros.Favorito.</summary>
public class FavoritoUsuarioRepository
{
    private readonly SupabaseContext _ctx;
    public FavoritoUsuarioRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<HashSet<int>> ObterIdsAsync(int usuarioId)
    {
        var resp = await _ctx.Client.From<FavoritosUsuarioModel>()
            .Filter("usuario_id", Operator.Equals, usuarioId.ToString())
            .Get();
        return resp.Models.Select(f => f.MacroId).ToHashSet();
    }

    /// <summary>Alterna o favorito pessoal e retorna o novo estado (true = favoritado).</summary>
    public async Task<bool> AlternarAsync(int usuarioId, int macroId)
    {
        var existente = await _ctx.Client.From<FavoritosUsuarioModel>()
            .Filter("usuario_id", Operator.Equals, usuarioId.ToString())
            .Filter("macro_id", Operator.Equals, macroId.ToString())
            .Single();

        if (existente != null)
        {
            await _ctx.Client.From<FavoritosUsuarioModel>().Filter("id", Operator.Equals, existente.Id.ToString()).Delete();
            return false;
        }

        await _ctx.Client.From<FavoritosUsuarioModel>().Insert(new FavoritosUsuarioModel
        {
            UsuarioId = usuarioId, MacroId = macroId, DataCriacao = DateTime.Now
        });
        return true;
    }
}
