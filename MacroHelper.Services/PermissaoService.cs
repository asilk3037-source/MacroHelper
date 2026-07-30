using MacroHelper.Core.Entities;

namespace MacroHelper.Services;

/// <summary>Chaves de permissão granular configuráveis por usuário (perfil "Usuario").</summary>
public static class Permissoes
{
    public const string ExcluirMacros         = "ExcluirMacros";
    public const string EditarMacrosDeOutros  = "EditarMacrosDeOutros";
    public const string GerenciarCategorias   = "GerenciarCategorias";
    public const string VerRelatorios         = "VerRelatorios";

    public static readonly (string Chave, string Label, string Descricao)[] Todas =
    [
        (ExcluirMacros,        "Excluir macros",            "Permite excluir qualquer macro da equipe."),
        (EditarMacrosDeOutros, "Editar macros de outros",   "Permite editar macros criadas por outros usuários."),
        (GerenciarCategorias,  "Gerenciar categorias",      "Permite criar, editar e excluir categorias."),
        (VerRelatorios,        "Ver relatórios da equipe",  "Permite ver o ranking e auditoria de todos os usuários."),
    ];

    public static bool Tem(Usuario? usuario, string chave)
    {
        if (usuario == null) return false;
        if (usuario.Perfil == "Auditor") return false;
        if (usuario.Perfil == "Admin") return true;

        if (usuario.PermissoesCustom == null) return false;

        var concedidas = usuario.PermissoesCustom
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return concedidas.Contains(chave);
    }
}
