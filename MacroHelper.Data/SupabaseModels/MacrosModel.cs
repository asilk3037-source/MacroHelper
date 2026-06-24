using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MacroHelper.Data.SupabaseModels;

[Table("macros")]
public class MacrosModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("atalho")]
    public string Atalho { get; set; } = string.Empty;

    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("conteudo")]
    public string Conteudo { get; set; } = string.Empty;

    [Column("categoria")]
    public string? Categoria { get; set; }

    [Column("categoria_id")]
    public int? CategoriaId { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }

    [Column("data_atualizacao")]
    public DateTime? DataAtualizacao { get; set; }

    [Column("favorito")]
    public bool Favorito { get; set; }

    [Column("atalho_tecla")]
    public string? AtalhoTecla { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Aprovada";

    [Column("criado_por_id")]
    public int? CriadoPorId { get; set; }

    [Column("grupos_permitidos")]
    public string? GruposPermitidos { get; set; }

    [Column("imagem_base64")]
    public string? ImagemBase64 { get; set; }

    [Column("publica")]
    public bool Publica { get; set; }

    [Column("total_curtidas")]
    public int TotalCurtidas { get; set; }
}
