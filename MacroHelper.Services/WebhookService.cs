using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;
using System.Net.Http;
using System.Net.Http.Json;

namespace MacroHelper.Services;

public static class EventosWebhook
{
    public const string MacroUsada      = "MacroUsada";
    public const string UsuarioCriado   = "UsuarioCriado";
    public const string BackupConcluido = "BackupConcluido";

    public static readonly string[] Todos = [MacroUsada, UsuarioCriado, BackupConcluido];
}

public class WebhookService
{
    private readonly WebhookRepository _repo;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public WebhookService(WebhookRepository repo) => _repo = repo;

    public async Task<IEnumerable<Webhook>> ObterTodosAsync() => await _repo.GetAllAsync();

    public async Task<(bool Ok, string Msg)> SalvarAsync(Webhook w)
    {
        if (string.IsNullOrWhiteSpace(w.Nome)) return (false, "Nome é obrigatório.");
        if (!Uri.TryCreate(w.Url, UriKind.Absolute, out _)) return (false, "URL inválida.");

        if (w.Id == 0) await _repo.InsertAsync(w);
        else           await _repo.UpdateAsync(w);
        return (true, "Webhook salvo!");
    }

    public async Task ExcluirAsync(int id) => await _repo.DeleteAsync(id);

    /// <summary>Dispara (best-effort, não bloqueia o app) todos os webhooks ativos cadastrados para o evento.</summary>
    public async Task DispararAsync(string evento, object payload)
    {
        try
        {
            var webhooks = (await _repo.GetAtivosPorEventoAsync(evento)).ToList();
            foreach (var w in webhooks)
                _ = EnviarAsync(w.Url, evento, payload);
        }
        catch { /* nunca deve afetar o fluxo principal */ }
    }

    public async Task<(bool Ok, string Msg)> TestarAsync(Webhook w)
    {
        try
        {
            await EnviarAsync(w.Url, "Teste", new { mensagem = "Disparo de teste do SK MacroHelper." });
            return (true, "Disparado! Verifique o endpoint de destino.");
        }
        catch (Exception ex) { return (false, $"Falha ao disparar: {ex.Message}"); }
    }

    private static async Task EnviarAsync(string url, string evento, object payload)
    {
        try
        {
            await _http.PostAsJsonAsync(url, new { evento, data = DateTime.Now, payload });
        }
        catch { /* endpoint externo fora do ar não deve gerar erro visível */ }
    }
}
