namespace MacroHelper.Core.Entities;

public class IntimacaoErro
{
    public int       Id           { get; set; }
    public DateTime  DataEmail    { get; set; } = DateTime.Today;
    public string    Advogado     { get; set; } = string.Empty;
    public string    TipoLogin    { get; set; } = string.Empty;
    public string    Sistema      { get; set; } = string.Empty;
    public string    LinkTribunal { get; set; } = string.Empty;
    public string    Erro         { get; set; } = string.Empty;
    public string    Status       { get; set; } = "Novo";
    public string    Observacao   { get; set; } = string.Empty;
    public DateTime  CriadoEm    { get; set; } = DateTime.Now;
    public DateTime  AtualizadoEm { get; set; } = DateTime.Now;
    public DateTime? ResolvidoEm  { get; set; }
}
