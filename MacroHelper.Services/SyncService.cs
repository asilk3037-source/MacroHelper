using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MacroHelper.Core.Entities;

namespace MacroHelper.Services;

public class SyncService
{
    private readonly MacroService _macroService;
    public SyncService(MacroService macroService) => _macroService = macroService;

    public async Task<(bool Ok, string Mensagem)> VerificarServidorAsync(string servidorUrl)
    {
        if (string.IsNullOrWhiteSpace(servidorUrl))
            return (false, "Configure a URL do servidor.");

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            var resp = await http.GetAsync(servidorUrl.TrimEnd('/') + "/api/health");
            return resp.IsSuccessStatusCode ? (true, "Servidor online.") : (false, $"Servidor respondeu com erro ({(int)resp.StatusCode}).");
        }
        catch (Exception)
        {
            return (false, "Servidor inacessível.");
        }
    }

    public async Task<(bool Ok, string Mensagem)> SincronizarAsync(string servidorUrl, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(servidorUrl))
            return (false, "Configure a URL do servidor nas Configurações.");

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(servidorUrl.TrimEnd('/') + "/") };

            var loginResp = await http.PostAsJsonAsync("api/auth/login", new { email, senha });
            if (!loginResp.IsSuccessStatusCode)
                return (false, "Falha no login com o servidor de sincronização.");

            var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponseSync>();
            if (auth == null) return (false, "Resposta inválida do servidor.");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var remotoResp = await http.GetAsync("api/macros");
            if (!remotoResp.IsSuccessStatusCode)
                return (false, "Falha ao obter macros do servidor.");

            var remotas = await remotoResp.Content.ReadFromJsonAsync<List<MacroRemotaDto>>() ?? new();
            var locais  = (await _macroService.ObterTodosAsync()).ToList();

            var atalhosLocais  = locais.Select(m => m.Atalho).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var atalhosRemotos = remotas.Select(m => m.Atalho).ToHashSet(StringComparer.OrdinalIgnoreCase);

            int recebidas = 0, enviadas = 0;

            foreach (var r in remotas.Where(r => !atalhosLocais.Contains(r.Atalho)))
            {
                await _macroService.SalvarAsync(new Macro
                {
                    Atalho   = r.Atalho,
                    Titulo   = r.Titulo,
                    Conteudo = r.Conteudo,
                    Ativo    = r.Ativo,
                    Status   = "Aprovada"
                });
                recebidas++;
            }

            foreach (var l in locais.Where(l => !atalhosRemotos.Contains(l.Atalho)))
            {
                var resp = await http.PostAsJsonAsync("api/macros", new
                {
                    Id = 0, l.Atalho, l.Titulo, l.Conteudo, l.CategoriaId, l.Ativo
                });
                if (resp.IsSuccessStatusCode) enviadas++;
            }

            return (true, $"Sincronizado: {recebidas} recebida(s), {enviadas} enviada(s).");
        }
        catch (HttpRequestException)
        {
            return (false, "Não foi possível conectar ao servidor.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao sincronizar: {ex.Message}");
        }
    }

    private record AuthResponseSync(string Token, string Nome, string Perfil, int Id);
    private record MacroRemotaDto(int Id, string Atalho, string Titulo, string Conteudo, string? Categoria, int? CategoriaId, bool Ativo, DateTime DataCriacao);
}
