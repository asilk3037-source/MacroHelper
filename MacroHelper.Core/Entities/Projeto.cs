namespace MacroHelper.Core.Entities;

public class Projeto
{
    public int      Id               { get; set; }
    public string   Nome             { get; set; } = string.Empty;
    public string?  Descricao        { get; set; }
    public string   Status           { get; set; } = "Ativo"; // Ativo | Concluído | Cancelado
    public DateTime? DataInicio      { get; set; }
    public DateTime? DataFimPrevista { get; set; }
    public DateTime CriadoEm        { get; set; } = DateTime.Now;
}
