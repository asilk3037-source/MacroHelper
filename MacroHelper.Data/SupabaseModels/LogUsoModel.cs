using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("log_uso")]
public class LogUsoModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("macro_id")]
    public int? MacroId { get; set; }

    [Column("macro_titulo")]
    public string MacroTitulo { get; set; } = string.Empty;

    [Column("macro_atalho")]
    public string MacroAtalho { get; set; } = string.Empty;

    [Column("aplicativo")]
    public string? Aplicativo { get; set; }

    [Column("usuario_id")]
    public int? UsuarioId { get; set; }

    [Column("data_uso")]
    public DateTime DataUso { get; set; }

    [Column("caracteres")]
    public int Caracteres { get; set; }
}
