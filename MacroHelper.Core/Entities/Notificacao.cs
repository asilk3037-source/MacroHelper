namespace MacroHelper.Core.Entities;

public class Notificacao
{
    public int      Id        { get; set; }
    public string   Titulo    { get; set; } = string.Empty;
    public string   Mensagem  { get; set; } = string.Empty;
    public string   Tipo      { get; set; } = "Info"; // Info | Sucesso | Erro | Aviso
    public bool     Lida      { get; set; } = false;
    public DateTime Data      { get; set; } = DateTime.Now;

    public string GrupoData => Data.Date == DateTime.Today ? "HOJE"
        : Data.Date == DateTime.Today.AddDays(-1) ? "ONTEM"
        : Data.ToString("dd/MM/yyyy");
}
