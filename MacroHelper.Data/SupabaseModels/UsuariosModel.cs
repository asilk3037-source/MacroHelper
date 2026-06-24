using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("usuarios")]
public class UsuariosModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("auth_user_id")]
    public Guid? AuthUserId { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("perfil")]
    public string Perfil { get; set; } = "Usuario";

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }

    [Column("categorias_permitidas")]
    public string? CategoriasPermitidas { get; set; }

    [Column("grupo_id")]
    public int? GrupoId { get; set; }

    [Column("permissoes_custom")]
    public string? PermissoesCustom { get; set; }
}
