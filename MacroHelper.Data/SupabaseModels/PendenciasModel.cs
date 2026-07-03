using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("pendencias")]
public class PendenciasModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("projeto_id")]
    public int ProjetoId { get; set; }

    [Column("ata_id")]
    public int? AtaId { get; set; }

    [Column("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [Column("responsavel")]
    public string? Responsavel { get; set; }

    [Column("prazo")]
    public DateTime? Prazo { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Aberta";

    [Column("prioridade")]
    public string Prioridade { get; set; } = "Media";

    [Column("criado_em")]
    public DateTime CriadoEm { get; set; }

    [Column("atualizado_em")]
    public DateTime AtualizadoEm { get; set; }
}
