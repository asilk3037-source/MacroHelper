namespace MacroHelper.Core.Entities;

public class Macro
{
    public int      Id              { get; set; }
    public string   Atalho          { get; set; } = string.Empty;
    public string   Titulo          { get; set; } = string.Empty;
    public string   Conteudo        { get; set; } = string.Empty;
    public string?  Categoria       { get; set; }
    public int?     CategoriaId     { get; set; }
    public bool     Ativo           { get; set; } = true;
    public DateTime DataCriacao     { get; set; } = DateTime.Now;
    public DateTime? DataAtualizacao { get; set; }

    public bool     Favorito        { get; set; } = false;
    public string?  AtalhoTecla     { get; set; }              // dígito 1-9 para Ctrl+Alt+N
    public string   Status          { get; set; } = "Aprovada"; // Aprovada | Pendente
    public int?     CriadoPorId     { get; set; }

    // IDs de grupo separados por vírgula; null/vazio = acesso a todos os grupos
    public string?  GruposPermitidos { get; set; }
    public string?  ImagemBase64     { get; set; }

    // Preenchido em memória pelo MacroService a partir de FavoritosUsuario — não é uma coluna de Macros.
    public bool     FavoritoPessoal  { get; set; } = false;

    // Comunidade interna: macro compartilhada publicamente dentro da equipe.
    public bool     Publica          { get; set; } = false;
    public int      TotalCurtidas    { get; set; } = 0;

    // Preenchido em memória pelo MacroService a partir de MacroCurtidas — não é uma coluna de Macros.
    public bool     CurtidaPeloUsuario { get; set; } = false;
}
