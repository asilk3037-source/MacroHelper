using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MacroHelper.Mobile.Models;

namespace MacroHelper.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private string _baseUrl = string.Empty;
    private string _token   = string.Empty;

    public bool Autenticado => !string.IsNullOrEmpty(_token);

    public ApiService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        CarregarConfiguracoes();
    }

    // ── Auth ─────────────────────────────────────────────────────
    public async Task<(bool Ok, string Msg, UsuarioMobile? Usuario)> LoginAsync(string email, string senha)
    {
        try
        {
            var res = await PostAsync<AuthResponse>("auth/login", new { email, senha });
            if (res == null) return (false, "Erro de conexão", null);
            _token = res.Token;
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            SalvarToken(res.Token, res.Nome, res.Perfil, res.Id);
            return (true, "Login realizado!", new UsuarioMobile(res.Id, res.Nome, res.Perfil));
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool Ok, string Msg)> CriarContaAsync(
        string nome, string email, string senha, string confirmar)
    {
        try
        {
            var res = await PostAsync<AuthResponse>("auth/criar",
                new { nome, email, senha, confirmar, perfil = "Usuario" });
            if (res == null) return (false, "Erro ao criar conta");
            _token = res.Token;
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            SalvarToken(res.Token, res.Nome, res.Perfil, res.Id);
            return (true, "Conta criada!");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    // ── Macros ───────────────────────────────────────────────────
    public bool UltimaCargaOffline { get; private set; }
    private string CaminhoCache => Path.Combine(FileSystem.AppDataDirectory, "macros_cache.json");

    public async Task<List<MacroMobile>> ObterMacrosAsync(string? busca = null)
    {
        var url = "macros";
        if (!string.IsNullOrEmpty(busca)) url += $"?busca={Uri.EscapeDataString(busca)}";

        try
        {
            var resultado = await GetAsync<List<MacroMobile>>(url);
            if (resultado != null)
            {
                UltimaCargaOffline = false;
                if (string.IsNullOrEmpty(busca)) await SalvarCacheAsync(resultado);
                return resultado;
            }
        }
        catch { /* sem conexão: cai para o cache local */ }

        UltimaCargaOffline = true;
        var cache = await CarregarCacheAsync();
        if (string.IsNullOrWhiteSpace(busca)) return cache;
        return cache.Where(m =>
            m.Titulo.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
            m.Atalho.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
            m.Conteudo.Contains(busca, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task SalvarCacheAsync(List<MacroMobile> macros)
    {
        try { await File.WriteAllTextAsync(CaminhoCache, JsonSerializer.Serialize(macros)); }
        catch { /* cache é best-effort */ }
    }

    private async Task<List<MacroMobile>> CarregarCacheAsync()
    {
        try
        {
            if (!File.Exists(CaminhoCache)) return [];
            var json = await File.ReadAllTextAsync(CaminhoCache);
            return JsonSerializer.Deserialize<List<MacroMobile>>(json) ?? [];
        }
        catch { return []; }
    }

    public async Task<MacroMobile?> SalvarMacroAsync(MacroMobile macro)
        => await PostAsync<MacroMobile>("macros", new
        {
            macro.Id, macro.Atalho, macro.Titulo, macro.Conteudo,
            macro.Categoria, macro.CategoriaId, macro.Ativo
        });

    public async Task<bool> ExcluirMacroAsync(int id)
    {
        try
        {
            var res = await _http.DeleteAsync($"{_baseUrl}/api/macros/{id}");
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<string> GerarComIAAsync(string descricao, string? tom = null)
    {
        var res = await PostAsync<GerarResponse>("macros/gerar", new { descricao, tom });
        return res?.Conteudo ?? string.Empty;
    }

    public async Task<string> AjustarTomAsync(int id, string tom)
    {
        var res = await PostAsync<GerarResponse>($"macros/{id}/ajustar-tom", new { tom });
        return res?.Conteudo ?? string.Empty;
    }

    // ── Categorias ───────────────────────────────────────────────
    public async Task<List<CategoriaMobile>> ObterCategoriasAsync()
        => await GetAsync<List<CategoriaMobile>>("categorias") ?? [];


    public async Task<List<ViewModels.LogMobile>> ObterLogAsync()
    {
        var url = $"log?de={DateTime.Today.AddDays(-30):yyyy-MM-dd}&ate={DateTime.Today:yyyy-MM-dd}";
        return await GetAsync<List<ViewModels.LogMobile>>(url) ?? [];
    }

    // ── Log ──────────────────────────────────────────────────────
    public async Task RegistrarUsoAsync(int? macroId, string titulo, string atalho)
    {
        try { await PostAsync<object>("log", new { macroId, macroTitulo = titulo, macroAtalho = atalho, aplicativo = "Mobile" }); }
        catch { /* não bloqueia o usuário */ }
    }

    // ── Helpers ──────────────────────────────────────────────────
    private async Task<T?> GetAsync<T>(string endpoint)
    {
        var res  = await _http.GetAsync($"{_baseUrl}/api/{endpoint}");
        if (!res.IsSuccessStatusCode) return default;
        return await res.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> PostAsync<T>(string endpoint, object body)
    {
        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res     = await _http.PostAsync($"{_baseUrl}/api/{endpoint}", content);
        if (!res.IsSuccessStatusCode) return default;
        return await res.Content.ReadFromJsonAsync<T>();
    }

    private void CarregarConfiguracoes()
    {
        _baseUrl = Preferences.Get("api_url", "http://localhost:5000");
        _token   = Preferences.Get("auth_token", string.Empty);
        if (!string.IsNullOrEmpty(_token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
    }

    private static void SalvarToken(string token, string nome, string perfil, int id)
    {
        Preferences.Set("auth_token", token);
        Preferences.Set("user_nome",  nome);
        Preferences.Set("user_perfil", perfil);
        Preferences.Set("user_id",    id);
    }

    public void ConfigurarUrl(string url)
    {
        _baseUrl = url.TrimEnd('/');
        Preferences.Set("api_url", _baseUrl);
    }

    public void Logout()
    {
        _token = string.Empty;
        Preferences.Remove("auth_token");
        _http.DefaultRequestHeaders.Authorization = null;
    }
}

public record AuthResponse(string Token, string Nome, string Perfil, int Id);
public record GerarResponse(string Conteudo);

// Extension for log - added after class definition issue, appended here as partial
