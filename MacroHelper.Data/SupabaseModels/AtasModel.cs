using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("atas")]
public class AtasModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("projeto_id")]
    public int ProjetoId { get; set; }

    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("cliente")]
    public string? Cliente { get; set; }

    [Column("data_reuniao")]
    public DateTime DataReuniao { get; set; }

    [Column("horario")]
    public string? Horario { get; set; }

    [Column("participantes_netview")]
    public string? ParticipantesNetview { get; set; }

    [Column("participantes_cliente")]
    public string? ParticipantesCliente { get; set; }

    [Column("notas")]
    public string? Notas { get; set; }

    [Column("criado_em")]
    public DateTime CriadoEm { get; set; }
}
