using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class NotificacaoRepository
{
    private readonly SupabaseContext _ctx;
    public NotificacaoRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<int> InsertAsync(Notificacao n)
    {
        var inserted = await _ctx.Client.From<NotificacoesModel>().Insert(new NotificacoesModel
        {
            Titulo = n.Titulo, Mensagem = n.Mensagem, Tipo = n.Tipo, Lida = false, Data = DateTime.Now
        });
        return inserted.Models.First().Id;
    }

    public async Task<IEnumerable<Notificacao>> GetRecentesAsync(int limite = 100)
    {
        var resp = await _ctx.Client.From<NotificacoesModel>()
            .Order("data", Ordering.Descending)
            .Limit(limite)
            .Get();
        return resp.Models.Select(MapToNotificacao);
    }

    public async Task<int> ContarNaoLidasAsync()
    {
        var resp = await _ctx.Client.From<NotificacoesModel>()
            .Filter("lida", Operator.Equals, "false")
            .Get();
        return resp.Models.Count;
    }

    public async Task MarcarComoLidaAsync(int id)
    {
        var m = await _ctx.Client.From<NotificacoesModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.Lida = true;
        await _ctx.Client.From<NotificacoesModel>().Update(m);
    }

    public async Task MarcarTodasComoLidasAsync()
    {
        var resp = await _ctx.Client.From<NotificacoesModel>().Filter("lida", Operator.Equals, "false").Get();
        if (resp.Models.Count == 0) return;
        foreach (var m in resp.Models) m.Lida = true;
        await Task.WhenAll(resp.Models.Select(m => _ctx.Client.From<NotificacoesModel>().Update(m)));
    }

    public async Task LimparAsync()
    {
        var resp = await _ctx.Client.From<NotificacoesModel>().Get();
        if (resp.Models.Count == 0) return;
        var ids = resp.Models.Select(m => (object)m.Id).ToList();
        await _ctx.Client.From<NotificacoesModel>().Filter("id", Operator.In, ids).Delete();
    }

    private static Notificacao MapToNotificacao(NotificacoesModel m) => new()
    {
        Id = m.Id, Titulo = m.Titulo, Mensagem = m.Mensagem, Tipo = m.Tipo, Lida = m.Lida, Data = m.Data
    };
}
