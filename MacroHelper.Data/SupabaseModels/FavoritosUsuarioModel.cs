using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("favoritos_usuario")]
public class FavoritosUsuarioModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("macro_id")]
    public int MacroId { get; set; }

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }
}
