using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("permissoes_tela")]
public class PermissoesTelaModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("tela")]
    public string Tela { get; set; } = string.Empty;

    [Column("nivel")]
    public string Nivel { get; set; } = "Editar";

    [Column("data_atualizacao")]
    public DateTime DataAtualizacao { get; set; }
}
