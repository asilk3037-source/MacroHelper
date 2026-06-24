using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("macro_agendamentos")]
public class MacroAgendamentosModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("macro_id")]
    public int MacroId { get; set; }

    [Column("dias_semana")]
    public string DiasSemana { get; set; } = string.Empty;

    [Column("horario")]
    public string Horario { get; set; } = string.Empty;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Column("ultima_execucao")]
    public DateTime? UltimaExecucao { get; set; }

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }
}
