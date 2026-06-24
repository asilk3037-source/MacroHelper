using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MacroHelper.Services;

public class IaService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private string _apiKey = string.Empty;

    public string ApiKey
    {
        get => _apiKey;
        set => _apiKey = value ?? string.Empty;
    }

    public bool Configurado => !string.IsNullOrEmpty(_apiKey);

    public async Task<string> GerarConteudoMacroAsync(string descricao, string? tom = null)
    {
        var prompt = $"""
            Crie um texto profissional completo para uma macro de atendimento.

            Descrição: "{descricao}"
            Tom: {tom ?? "profissional e cordial"}

            Retorne APENAS o texto da macro, sem comentários, pronto para usar.
            Máximo 150 palavras. Termine com saudação e "Atenciosamente, Equipe".
            """;
        return await ChamarClaudeAsync(prompt);
    }

    public async Task<string> AjustarTomAsync(string conteudo, string tom)
    {
        var instrucao = tom.ToLower() switch
        {
            "formal"   => "mais formal e profissional",
            "informal" => "mais amigável e descontraído, mas ainda respeitoso",
            "curto"    => "muito mais conciso, mantendo o essencial",
            "empatico" => "mais empático e acolhedor",
            _          => tom
        };

        var prompt = $"""
            Reescreva este texto de forma {instrucao}:

            {conteudo}

            Retorne APENAS o texto reescrito, sem comentários.
            """;
        return await ChamarClaudeAsync(prompt);
    }

    public async Task<string> SugerirAtalhoAsync(string titulo)
    {
        var prompt = $"""
            Sugira um atalho curto (2-4 palavras com hífens) para uma macro de título: "{titulo}"
            Exemplos: chamado-recebido, aguardando-retorno, resolucao-ok
            Retorne APENAS o atalho, minúsculas, sem espaços, sem pontuação.
            """;
        return await ChamarClaudeAsync(prompt);
    }

    private async Task<string> ChamarClaudeAsync(string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return "Configure sua chave da API Anthropic nas Configurações do app.";

        try
        {
            var payload = new
            {
                model      = "claude-sonnet-4-20250514",
                max_tokens = 600,
                messages   = new[] { new { role = "user", content = prompt } }
            };

            var req = new HttpRequestMessage(HttpMethod.Post,
                "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", _apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res  = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return $"Erro da API ({res.StatusCode}): {json}";

            var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            return $"Erro ao contatar IA: {ex.Message}";
        }
    }
}
