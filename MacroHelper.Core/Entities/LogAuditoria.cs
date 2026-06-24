namespace MacroHelper.Core.Entities;

public class LogAuditoria
{
    public int      Id         { get; set; }
    public int?     UsuarioId  { get; set; }
    public string   Acao       { get; set; } = string.Empty;
    public string   Entidade   { get; set; } = string.Empty;
    public int?     EntidadeId { get; set; }
    public string?  Detalhes   { get; set; }
    public DateTime Data       { get; set; } = DateTime.Now;
}
