namespace MacroHelper.Core.Entities;

public class Webhook
{
    public int      Id          { get; set; }
    public string   Nome        { get; set; } = string.Empty;
    public string   Url         { get; set; } = string.Empty;
    public string   Evento      { get; set; } = "MacroUsada"; // MacroUsada | UsuarioCriado | BackupConcluido
    public bool     Ativo       { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
