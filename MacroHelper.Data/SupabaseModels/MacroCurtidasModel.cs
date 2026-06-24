using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("macro_curtidas")]
public class MacroCurtidasModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("macro_id")]
    public int MacroId { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }
}
