namespace MacroHelper.Core.Entities;

public class Usuario
{
    public int      Id          { get; set; }
    public string   Nome        { get; set; } = string.Empty;
    public string   Email       { get; set; } = string.Empty;
    public string   SenhaHash   { get; set; } = string.Empty;
    public string   Perfil      { get; set; } = "Usuario"; // Admin | Usuario
    public bool     Ativo       { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    // IDs de categoria separados por vírgula; null/vazio = acesso a todas
    public string?  CategoriasPermitidas { get; set; }
    public int?     GrupoId { get; set; }

    // Chaves de permissão granular separadas por vírgula (ex: "CriarMacros,EditarMacros").
    // null = usa o padrão do Perfil (Admin = tudo; Usuario = criar e usar macros, sem excluir/gerenciar).
    public string?  PermissoesCustom { get; set; }
}
