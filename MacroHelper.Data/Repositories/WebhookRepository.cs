using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class WebhookRepository
{
    private readonly SupabaseContext _ctx;
    public WebhookRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Webhook>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<WebhooksModel>().Order("nome", Ordering.Ascending).Get();
        return resp.Models.Select(MapToWebhook);
    }

    public async Task<IEnumerable<Webhook>> GetAtivosPorEventoAsync(string evento)
    {
        var resp = await _ctx.Client.From<WebhooksModel>()
            .Filter("ativo", Operator.Equals, "true")
            .Filter("evento", Operator.Equals, evento)
            .Get();
        return resp.Models.Select(MapToWebhook);
    }

    public async Task<int> InsertAsync(Webhook w)
    {
        var inserted = await _ctx.Client.From<WebhooksModel>().Insert(new WebhooksModel
        {
            Nome = w.Nome, Url = w.Url, Evento = w.Evento, Ativo = w.Ativo, DataCriacao = DateTime.Now
        });
        return inserted.Models.First().Id;
    }

    public async Task UpdateAsync(Webhook w)
    {
        var m = await _ctx.Client.From<WebhooksModel>().Filter("id", Operator.Equals, w.Id.ToString()).Single();
        if (m == null) return;
        m.Nome = w.Nome; m.Url = w.Url; m.Evento = w.Evento; m.Ativo = w.Ativo;
        await _ctx.Client.From<WebhooksModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        await _ctx.Client.From<WebhooksModel>().Filter("id", Operator.Equals, id.ToString()).Delete();
    }

    private static Webhook MapToWebhook(WebhooksModel m) => new()
    {
        Id = m.Id, Nome = m.Nome, Url = m.Url, Evento = m.Evento, Ativo = m.Ativo, DataCriacao = m.DataCriacao
    };
}
