using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("macro_versoes")]
public class MacroVersoesModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("macro_id")]
    public int MacroId { get; set; }

    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("conteudo")]
    public string Conteudo { get; set; } = string.Empty;

    [Column("modificado_por_id")]
    public int? ModificadoPorId { get; set; }

    [Column("data_modificacao")]
    public DateTime DataModificacao { get; set; }
}
