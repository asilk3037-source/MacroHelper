namespace MacroHelper.Core.Entities;

public class Pendencia
{
    public int      Id          { get; set; }
    public int      ProjetoId   { get; set; }
    public int?     AtaId       { get; set; }
    public string   Descricao   { get; set; } = string.Empty;
    public string?  Responsavel { get; set; }
    public DateTime? Prazo      { get; set; }
    public string   Status      { get; set; } = "Aberta";  // Aberta | Em andamento | Concluída | Cancelada
    public string   Prioridade  { get; set; } = "Media";   // Alta | Media | Baixa
    public DateTime CriadoEm   { get; set; } = DateTime.Now;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
}
