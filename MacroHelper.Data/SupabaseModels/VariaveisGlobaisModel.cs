using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("variaveis_globais")]
public class VariaveisGlobaisModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("valor_padrao")]
    public string ValorPadrao { get; set; } = string.Empty;

    [Column("descricao")]
    public string? Descricao { get; set; }

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }
}
