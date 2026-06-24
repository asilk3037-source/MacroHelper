using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class LogAuditoriaRepository
{
    private readonly SupabaseContext _ctx;
    public LogAuditoriaRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task RegistrarAsync(int? usuarioId, string acao, string entidade, int? entidadeId, string? detalhes)
    {
        await _ctx.Client.From<LogAuditoriaModel>().Insert(new LogAuditoriaModel
        {
            UsuarioId = usuarioId, Acao = acao, Entidade = entidade,
            EntidadeId = entidadeId, Detalhes = detalhes, Data = DateTime.Now
        });
    }

    public async Task<IEnumerable<LogAuditoria>> GetRecentesAsync(int limite = 200)
    {
        var resp = await _ctx.Client.From<LogAuditoriaModel>()
            .Order("data", Ordering.Descending)
            .Limit(limite)
            .Get();
        return resp.Models.Select(MapToLogAuditoria);
    }

    public async Task<IEnumerable<(DateTime Data, string Acao, string Entidade, string? Detalhes, string UsuarioNome)>> GetRecentesComNomeAsync(int limite = 200)
    {
        var resp = await _ctx.Client.From<LogAuditoriaModel>()
            .Order("data", Ordering.Descending)
            .Limit(limite)
            .Get();

        var usuarioIds = resp.Models.Where(a => a.UsuarioId.HasValue).Select(a => a.UsuarioId!.Value).Distinct().ToList();
        var nomesPorId = new Dictionary<int, string>();
        if (usuarioIds.Count > 0)
        {
            var usuarios = await _ctx.Client.From<UsuariosModel>()
                .Filter("id", Operator.In, usuarioIds.Select(id => (object)id).ToList())
                .Get();
            nomesPorId = usuarios.Models.ToDictionary(u => u.Id, u => u.Nome);
        }

        return resp.Models.Select(a => (
            a.Data, a.Acao, a.Entidade, a.Detalhes,
            a.UsuarioId.HasValue && nomesPorId.TryGetValue(a.UsuarioId.Value, out var nome) ? nome : "Sistema"));
    }

    private static LogAuditoria MapToLogAuditoria(LogAuditoriaModel m) => new()
    {
        Id = m.Id, UsuarioId = m.UsuarioId, Acao = m.Acao, Entidade = m.Entidade,
        EntidadeId = m.EntidadeId, Detalhes = m.Detalhes, Data = m.Data
    };
}
