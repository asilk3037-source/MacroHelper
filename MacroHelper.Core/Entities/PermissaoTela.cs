namespace MacroHelper.Core.Entities;

/// <summary>Nível de acesso de um usuário a uma tela específica do app (chave igual a NavItem.Chave).</summary>
public class PermissaoTela
{
    public int    Id        { get; set; }
    public int    UsuarioId { get; set; }
    public string Tela      { get; set; } = string.Empty;
    public string Nivel     { get; set; } = string.Empty;
}
