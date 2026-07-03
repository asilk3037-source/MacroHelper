using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("projetos")]
public class ProjetosModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("descricao")]
    public string? Descricao { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Ativo";

    [Column("data_inicio")]
    public DateTime? DataInicio { get; set; }

    [Column("data_fim_prevista")]
    public DateTime? DataFimPrevista { get; set; }

    [Column("criado_em")]
    public DateTime CriadoEm { get; set; }
}
