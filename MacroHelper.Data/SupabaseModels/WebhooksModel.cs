using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("webhooks")]
public class WebhooksModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("evento")]
    public string Evento { get; set; } = "MacroUsada";

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }
}
