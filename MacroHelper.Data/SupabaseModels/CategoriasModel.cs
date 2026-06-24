using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("categorias")]
public class CategoriasModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("icone")]
    public string? Icone { get; set; }

    [Column("cor")]
    public string? Cor { get; set; }

    [Column("pai_id")]
    public int? PaiId { get; set; }

    [Column("ordem")]
    public int Ordem { get; set; }

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }
}
