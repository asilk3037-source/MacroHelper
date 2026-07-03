namespace MacroHelper.Core.Entities;

public class Ata
{
    public int      Id                      { get; set; }
    public int      ProjetoId               { get; set; }
    public string   Titulo                  { get; set; } = string.Empty;
    public string?  Cliente                 { get; set; }
    public DateTime DataReuniao             { get; set; }
    public string?  Horario                 { get; set; }
    public string?  ParticipantesNetview    { get; set; }
    public string?  ParticipantesCliente    { get; set; }
    public string?  Notas                   { get; set; }
    public DateTime CriadoEm               { get; set; } = DateTime.Now;
}
