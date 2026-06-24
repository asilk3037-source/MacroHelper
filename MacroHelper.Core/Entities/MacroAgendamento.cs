namespace MacroHelper.Core.Entities;

public class MacroAgendamento
{
    public int      Id              { get; set; }
    public int      MacroId         { get; set; }
    public string?  MacroTitulo     { get; set; } // preenchido via JOIN, não é coluna própria
    public string   DiasSemana      { get; set; } = string.Empty; // CSV: "1,2,3,4,5" (0=domingo..6=sabado)
    public string   Horario         { get; set; } = "09:00";      // HH:mm
    public bool     Ativo           { get; set; } = true;
    public DateTime? UltimaExecucao { get; set; }
    public DateTime DataCriacao     { get; set; } = DateTime.Now;
}
