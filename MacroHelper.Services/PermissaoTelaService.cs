using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

/// <summary>Níveis de acesso configuráveis por tela, em ordem crescente de poder.</summary>
public static class NiveisPermissaoTela
{
    public const string Nenhum     = "Nenhum";
    public const string Visualizar = "Visualizar";
    public const string Editar     = "Editar";
    public const string Excluir    = "Excluir";

    public static readonly string[] Ordem = [Nenhum, Visualizar, Editar, Excluir];

    public static int Ordinal(string? nivel) => nivel == null ? -1 : Array.IndexOf(Ordem, nivel);

    /// <summary>True se o nível atual atende (é igual ou superior a) o nível mínimo exigido.</summary>
    public static bool Atende(string? nivelAtual, string nivelMinimo) => Ordinal(nivelAtual) >= Ordinal(nivelMinimo);
}

/// <summary>Telas do app que podem ter visibilidade/nível de acesso configurados por usuário.
/// A chave deve ser igual à Chave usada em NavItem (MainViewModel.ConstruirNav).</summary>
public static class TelasApp
{
    public static readonly (string Chave, string Label)[] Todas =
    [
        ("dashboard",     "Início"),
        ("macros",        "Macros"),
        ("historico",     "Histórico de Versões"),
        ("categorias",    "Categorias"),
        ("variaveis",     "Variáveis Globais"),
        ("agendamentos",  "Agendamentos"),
        ("comunidade",    "Comunidade"),
        ("relatorio",     "Relatório"),
        ("usuarios",      "Usuários"),
        ("grupos",        "Grupos"),
        ("permissoes",    "Permissões"),
        ("auditoria",     "Auditoria"),
        ("integracoes",   "Integrações"),
        ("notificacoes",  "Notificações"),
        ("configuracoes", "Configurações"),
        ("ajuda",         "Ajuda"),
    ];

    /// <summary>Telas que só ficam visíveis por padrão para Admins (comportamento histórico do app).</summary>
    public static readonly HashSet<string> EquipeAdminPorPadrao =
        ["usuarios", "grupos", "permissoes", "auditoria", "integracoes"];
}

public class PermissaoTelaService
{
    private readonly PermissaoTelaRepository _repo;
    private readonly Dictionary<int, Dictionary<string, string>> _cache = new();

    public PermissaoTelaService(PermissaoTelaRepository repo) => _repo = repo;

    public async Task<Dictionary<string, string>> ObterMapaAsync(int usuarioId)
    {
        if (_cache.TryGetValue(usuarioId, out var cached)) return cached;
        var mapa = (await _repo.ObterPorUsuarioAsync(usuarioId)).ToDictionary(p => p.Tela, p => p.Nivel);
        _cache[usuarioId] = mapa;
        return mapa;
    }

    public async Task DefinirAsync(int usuarioId, string tela, string nivel)
    {
        await _repo.DefinirAsync(usuarioId, tela, nivel);
        _cache.Remove(usuarioId);
    }

    /// <summary>Nível efetivo de acesso de um usuário a uma tela, já considerando perfil Admin
    /// (sempre liberado) e os padrões históricos (telas de Equipe ocultas por padrão para quem não é Admin).</summary>
    public async Task<string> ObterNivelEfetivoAsync(Usuario usuario, string tela)
    {
        var mapa = usuario.Perfil == "Admin" ? null : await ObterMapaAsync(usuario.Id);
        return NivelEfetivo(usuario, tela, mapa);
    }

    public static string NivelEfetivo(Usuario usuario, string tela, Dictionary<string, string>? mapa)
    {
        if (usuario.Perfil == "Admin") return NiveisPermissaoTela.Excluir;
        if (tela == "perfil") return NiveisPermissaoTela.Editar;

        if (mapa != null && mapa.TryGetValue(tela, out var nivel)) return nivel;

        return TelasApp.EquipeAdminPorPadrao.Contains(tela)
            ? NiveisPermissaoTela.Nenhum
            : NiveisPermissaoTela.Editar;
    }
}
