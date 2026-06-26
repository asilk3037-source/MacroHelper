using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;
using System.Net.Http;
using System.Net.Http.Json;

namespace MacroHelper.Services;

public static class EventosWebhook
{
    public const string MacroUsada      = "MacroUsada";
    public const string UsuarioCriado   = "UsuarioCriado";
    public const string MacroCriada     = "MacroCriada";
    public const string MacroExcluida   = "MacroExcluida";
    public const string UsuarioExcluido = "UsuarioExcluido";
    public const string LoginFalhou     = "LoginFalhou";

    public static readonly string[] Todos =
        [MacroUsada, MacroCriada, MacroExcluida, UsuarioCriado, UsuarioExcluido, LoginFalhou];

    public static readonly Dictionary<string, string> Rotulos = new()
    {
        [MacroUsada]      = "Macro usada",
        [MacroCriada]     = "Macro criada",
        [MacroExcluida]   = "Macro excluída",
        [UsuarioCriado]   = "Usuário criado",
        [UsuarioExcluido] = "Usuário excluído",
        [LoginFalhou]     = "Login falhou",
    };

    public static string Rotulo(string evento) => Rotulos.TryGetValue(evento, out var r) ? r : evento;
}

public class WebhookService
{
    private readonly WebhookRepository _repo;
    private readonly LogAuditoriaRepository? _auditoriaRepo;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public WebhookService(WebhookRepository repo, LogAuditoriaRepository? auditoriaRepo = null)
    {
        _repo = repo;
        _auditoriaRepo = auditoriaRepo;
    }

    public async Task<IEnumerable<Webhook>> ObterTodosAsync() => await _repo.GetAllAsync();

    public async Task<(bool Ok, string Msg)> SalvarAsync(Webhook w, int? usuarioId = null)
    {
        if (string.IsNullOrWhiteSpace(w.Nome)) return (false, "Nome é obrigatório.");
        if (!Uri.TryCreate(w.Url, UriKind.Absolute, out _)) return (false, "URL inválida.");

        var ehNovo = w.Id == 0;
        if (ehNovo) await _repo.InsertAsync(w);
        else        await _repo.UpdateAsync(w);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(usuarioId, ehNovo ? "Criar" : "Editar", "Webhook", w.Id, $"{w.Nome} ({w.Evento})");
        return (true, "Webhook salvo!");
    }

    public async Task ExcluirAsync(int id, int? usuarioId = null)
    {
        await _repo.DeleteAsync(id);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(usuarioId, "Excluir", "Webhook", id, null);
    }

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
