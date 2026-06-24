using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("grupos")]
public class GruposModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }
}
