using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("notificacoes")]
public class NotificacoesModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("mensagem")]
    public string Mensagem { get; set; } = string.Empty;

    [Column("tipo")]
    public string Tipo { get; set; } = "Info";

    [Column("lida")]
    public bool Lida { get; set; }

    [Column("data")]
    public DateTime Data { get; set; }
}
