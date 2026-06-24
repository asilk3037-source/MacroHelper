namespace MacroHelper.Services;

/// <summary>
/// Estado compartilhado do modo totem/kiosk (tela cheia sem sidebar, para terminais de atendimento
/// compartilhados). Permite que Configurações altere o modo e a MainWindow reaja, mesmo sendo
/// instâncias de ViewModel diferentes.
/// </summary>
public class KioskModeService
{
    public bool Ativo { get; private set; }
    public event EventHandler<bool>? Alterado;

    public void Definir(bool ativo)
    {
        if (Ativo == ativo) return;
        Ativo = ativo;
        Alterado?.Invoke(this, ativo);
    }
}
