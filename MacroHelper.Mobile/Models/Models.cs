namespace MacroHelper.Mobile.Models;

public class MacroMobile
{
    public int     Id          { get; set; }
    public string  Atalho      { get; set; } = string.Empty;
    public string  Titulo      { get; set; } = string.Empty;
    public string  Conteudo    { get; set; } = string.Empty;
    public string? Categoria   { get; set; }
    public int?    CategoriaId { get; set; }
    public bool    Ativo       { get; set; } = true;
    public DateTime DataCriacao { get; set; }
}

public class CategoriaMobile
{
    public int     Id    { get; set; }
    public string  Nome  { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public string? Cor   { get; set; }
    public int?    PaiId { get; set; }
    public List<CategoriaMobile> Subcategorias { get; set; } = new();
}

public record UsuarioMobile(int Id, string Nome, string Perfil);
