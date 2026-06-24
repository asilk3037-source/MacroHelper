using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("log_auditoria")]
public class LogAuditoriaModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("usuario_id")]
    public int? UsuarioId { get; set; }

    [Column("acao")]
    public string Acao { get; set; } = string.Empty;

    [Column("entidade")]
    public string Entidade { get; set; } = string.Empty;

    [Column("entidade_id")]
    public int? EntidadeId { get; set; }

    [Column("detalhes")]
    public string? Detalhes { get; set; }

    [Column("data")]
    public DateTime Data { get; set; }
}
