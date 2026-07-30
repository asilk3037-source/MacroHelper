using MacroHelper.Core.Entities;
using MacroHelper.Core.Interfaces;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class MacroRepository : IMacroRepository
{
    private readonly SupabaseContext _ctx;
    public MacroRepository(SupabaseContext ctx) => _ctx = ctx;

    // Colunas carregadas em queries de lista — exclui imagem_base64 (pode ser centenas de KB por macro)
    private const string ColunasLista =
        "id,atalho,titulo,conteudo,categoria,categoria_id,ativo,data_criacao,data_atualizacao," +
        "favorito,atalho_tecla,status,criado_por_id,grupos_permitidos,publica,total_curtidas";

    public async Task<IEnumerable<Macro>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<MacrosModel>()
            .Select(ColunasLista)
            .Order("categoria", Ordering.Ascending)
            .Order("titulo", Ordering.Ascending)
            .Get();
        return resp.Models.Select(MapToMacro);
    }

    /// <summary>
    /// O ranking por CASE do SQL original não tem equivalente fluente no Postgrest — busca-se
    /// um conjunto filtrado mais amplo via OR de ilike e ranqueia/ordena em C#, preservando
    /// exatamente os mesmos 6 níveis de relevância de antes.
    /// </summary>
    public async Task<IEnumerable<Macro>> SearchAsync(string termo)
    {
        var like = $"%{termo}%";

        var resp = await _ctx.Client.From<MacrosModel>()
            .Select(ColunasLista)
            .Filter("ativo", Operator.Equals, "true")
            .Filter("status", Operator.Equals, "Aprovada")
            .Or(new List<Supabase.Postgrest.Interfaces.IPostgrestQueryFilter>
            {
                new Supabase.Postgrest.QueryFilter("atalho",    Operator.ILike, like),
                new Supabase.Postgrest.QueryFilter("titulo",    Operator.ILike, like),
                new Supabase.Postgrest.QueryFilter("conteudo",  Operator.ILike, like),
                new Supabase.Postgrest.QueryFilter("categoria", Operator.ILike, like),
            })
            .Get();

        int Relevancia(MacrosModel m) =>
            string.Equals(m.Atalho, termo, StringComparison.OrdinalIgnoreCase)                      ? 0 :
            m.Atalho.StartsWith(termo, StringComparison.OrdinalIgnoreCase)                            ? 1 :
            m.Titulo.StartsWith(termo, StringComparison.OrdinalIgnoreCase)                            ? 2 :
            m.Titulo.Contains(termo, StringComparison.OrdinalIgnoreCase)                              ? 3 :
            (m.Categoria?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false)               ? 4 :
            m.Conteudo.Contains(termo, StringComparison.OrdinalIgnoreCase)                             ? 5 : 6;

        return resp.Models
            .OrderBy(Relevancia)
            .ThenByDescending(m => m.Favorito)
            .ThenBy(m => m.Titulo, StringComparer.OrdinalIgnoreCase)
            .Select(MapToMacro);
    }

    public async Task<IEnumerable<Macro>> GetByCategoriaAsync(string categoria)
    {
        var resp = await _ctx.Client.From<MacrosModel>()
            .Select(ColunasLista)
            .Filter("categoria", Operator.Equals, categoria)
            .Filter("ativo", Operator.Equals, "true")
            .Filter("status", Operator.Equals, "Aprovada")
            .Order("favorito", Ordering.Descending)
            .Order("titulo", Ordering.Ascending)
            .Get();
        return resp.Models.Select(MapToMacro);
    }

    public async Task<Macro?> GetByIdAsync(int id)
    {
        var m = await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        return m == null ? null : MapToMacro(m);
    }

    public async Task<Macro?> GetByAtalhoAsync(string atalho)
    {
        var m = await _ctx.Client.From<MacrosModel>()
            .Filter("atalho", Operator.Equals, atalho)
            .Filter("ativo", Operator.Equals, "true")
            .Filter("status", Operator.Equals, "Aprovada")
            .Single();
        return m == null ? null : MapToMacro(m);
    }

    public async Task<Macro?> GetByAtalhoTeclaAsync(string digito)
    {
        var m = await _ctx.Client.From<MacrosModel>()
            .Filter("atalho_tecla", Operator.Equals, digito)
            .Filter("ativo", Operator.Equals, "true")
            .Filter("status", Operator.Equals, "Aprovada")
            .Single();
        return m == null ? null : MapToMacro(m);
    }

    public async Task<IEnumerable<Macro>> GetFavoritosAsync()
    {
        var resp = await _ctx.Client.From<MacrosModel>()
            .Select(ColunasLista)
            .Filter("favorito", Operator.Equals, "true")
            .Filter("ativo", Operator.Equals, "true")
            .Filter("status", Operator.Equals, "Aprovada")
            .Order("titulo", Ordering.Ascending)
            .Get();
        return resp.Models.Select(MapToMacro);
    }

    public async Task<IEnumerable<Macro>> GetPendentesAsync()
    {
        var resp = await _ctx.Client.From<MacrosModel>()
            .Filter("status", Operator.Equals, "Pendente")
            .Order("data_criacao", Ordering.Descending)
            .Get();
        return resp.Models.Select(MapToMacro);
    }

    public async Task<int> InsertAsync(Macro macro)
    {
        var model = MapToModel(macro);
        model.DataCriacao = DateTime.Now;
        var inserted = await _ctx.Client.From<MacrosModel>().Insert(model);
        return inserted.Models.First().Id;
    }

    public async Task UpdateAsync(Macro macro)
    {
        var m = await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, macro.Id.ToString()).Single();
        if (m == null) return;

        m.Atalho = macro.Atalho; m.Titulo = macro.Titulo; m.Conteudo = macro.Conteudo;
        m.Categoria = macro.Categoria; m.CategoriaId = macro.CategoriaId;
        m.Ativo = macro.Ativo; m.DataAtualizacao = DateTime.Now;
        m.Favorito = macro.Favorito; m.AtalhoTecla = macro.AtalhoTecla; m.Status = macro.Status;
        m.GruposPermitidos = macro.GruposPermitidos; m.ImagemBase64 = macro.ImagemBase64;

        await _ctx.Client.From<MacrosModel>().Update(m);
    }

    public async Task ToggleFavoritoAsync(int id, bool favorito)
    {
        var m = await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.Favorito = favorito;
        await _ctx.Client.From<MacrosModel>().Update(m);
    }

    public async Task ToggleAtivoAsync(int id, bool ativo)
    {
        var m = await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.Ativo = ativo;
        await _ctx.Client.From<MacrosModel>().Update(m);
    }

    public async Task AtualizarStatusAsync(int id, string status)
    {
        var m = await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.Status = status;
        await _ctx.Client.From<MacrosModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, id.ToString()).Delete();
    }

    public async Task<IEnumerable<string>> GetCategoriasAsync()
    {
        var resp = await _ctx.Client.From<MacrosModel>().Select("categoria").Get();
        return resp.Models
            .Select(m => m.Categoria)
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!)
            .Distinct()
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase);
    }

    // ── Comunidade interna ──────────────────────────────────────────
    public async Task<IEnumerable<Macro>> GetPublicasAsync()
    {
        var resp = await _ctx.Client.From<MacrosModel>()
            .Select(ColunasLista)
            .Filter("publica", Operator.Equals, "true")
            .Filter("ativo", Operator.Equals, "true")
            .Filter("status", Operator.Equals, "Aprovada")
            .Order("total_curtidas", Ordering.Descending)
            .Order("titulo", Ordering.Ascending)
            .Get();
        return resp.Models.Select(MapToMacro);
    }

    public async Task TogglePublicaAsync(int id, bool publica)
    {
        var m = await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.Publica = publica;
        await _ctx.Client.From<MacrosModel>().Update(m);
    }

    public async Task<bool> ToggleCurtidaAsync(int macroId, int usuarioId)
    {
        var existente = await _ctx.Client.From<MacroCurtidasModel>()
            .Filter("macro_id", Operator.Equals, macroId.ToString())
            .Filter("usuario_id", Operator.Equals, usuarioId.ToString())
            .Single();

        var macro = await _ctx.Client.From<MacrosModel>().Filter("id", Operator.Equals, macroId.ToString()).Single();

        if (existente != null)
        {
            await _ctx.Client.From<MacroCurtidasModel>().Filter("id", Operator.Equals, existente.Id.ToString()).Delete();
            if (macro != null)
            {
                macro.TotalCurtidas = Math.Max(0, macro.TotalCurtidas - 1);
                await _ctx.Client.From<MacrosModel>().Update(macro);
            }
            return false;
        }

        await _ctx.Client.From<MacroCurtidasModel>().Insert(new MacroCurtidasModel
        {
            MacroId = macroId, UsuarioId = usuarioId, DataCriacao = DateTime.Now
        });
        if (macro != null)
        {
            macro.TotalCurtidas += 1;
            await _ctx.Client.From<MacrosModel>().Update(macro);
        }
        return true;
    }

    public async Task<HashSet<int>> ObterCurtidasDoUsuarioAsync(int usuarioId)
    {
        var resp = await _ctx.Client.From<MacroCurtidasModel>()
            .Filter("usuario_id", Operator.Equals, usuarioId.ToString())
            .Get();
        return resp.Models.Select(c => c.MacroId).ToHashSet();
    }

    private static MacrosModel MapToModel(Macro macro) => new()
    {
        Id = macro.Id, Atalho = macro.Atalho, Titulo = macro.Titulo, Conteudo = macro.Conteudo,
        Categoria = macro.Categoria, CategoriaId = macro.CategoriaId, Ativo = macro.Ativo,
        DataCriacao = macro.DataCriacao, DataAtualizacao = macro.DataAtualizacao,
        Favorito = macro.Favorito, AtalhoTecla = macro.AtalhoTecla, Status = macro.Status,
        CriadoPorId = macro.CriadoPorId, GruposPermitidos = macro.GruposPermitidos,
        ImagemBase64 = macro.ImagemBase64, Publica = macro.Publica, TotalCurtidas = macro.TotalCurtidas
    };

    private static Macro MapToMacro(MacrosModel m) => new()
    {
        Id = m.Id, Atalho = m.Atalho, Titulo = m.Titulo, Conteudo = m.Conteudo,
        Categoria = m.Categoria, CategoriaId = m.CategoriaId, Ativo = m.Ativo,
        DataCriacao = m.DataCriacao, DataAtualizacao = m.DataAtualizacao,
        Favorito = m.Favorito, AtalhoTecla = m.AtalhoTecla, Status = m.Status,
        CriadoPorId = m.CriadoPorId, GruposPermitidos = m.GruposPermitidos,
        ImagemBase64 = m.ImagemBase64, Publica = m.Publica, TotalCurtidas = m.TotalCurtidas
    };
}
