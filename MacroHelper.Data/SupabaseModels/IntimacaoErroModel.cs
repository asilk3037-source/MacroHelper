using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("intimacoes_erros")]
public class IntimacaoErroModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("data_email")]
    public DateTime DataEmail { get; set; }

    [Column("advogado")]
    public string Advogado { get; set; } = string.Empty;

    [Column("tipo_login")]
    public string TipoLogin { get; set; } = string.Empty;

    [Column("sistema")]
    public string Sistema { get; set; } = string.Empty;

    [Column("link_tribunal")]
    public string LinkTribunal { get; set; } = string.Empty;

    [Column("erro")]
    public string Erro { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "Novo";

    [Column("observacao")]
    public string Observacao { get; set; } = string.Empty;

    [Column("criado_em")]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    [Column("atualizado_em")]
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    [Column("resolvido_em")]
    public DateTime? ResolvidoEm { get; set; }
}
