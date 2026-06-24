using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("log_uso_resumo")]
public class LogUsoResumoModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("ano_mes")]
    public string AnoMes { get; set; } = string.Empty;

    [Column("macro_titulo")]
    public string MacroTitulo { get; set; } = string.Empty;

    [Column("macro_atalho")]
    public string MacroAtalho { get; set; } = string.Empty;

    [Column("total")]
    public int Total { get; set; }

    [Column("caracteres")]
    public int Caracteres { get; set; }
}
