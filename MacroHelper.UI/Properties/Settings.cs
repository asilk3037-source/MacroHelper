using System.Configuration;

namespace MacroHelper.UI.Properties;

internal sealed class Settings : ApplicationSettingsBase
{
    private static readonly Settings _default =
        (Settings)Synchronized(new Settings());
    public static Settings Default => _default;

    [UserScopedSetting, DefaultSettingValue("Escuro")]
    public string Tema
    {
        get => (string)(this["Tema"] ?? "Escuro");
        set => this["Tema"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("False")]
    public bool IniciarComWindows
    {
        get => (bool)(this["IniciarComWindows"] ?? false);
        set => this["IniciarComWindows"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("True")]
    public bool MinimizarParaBandeja
    {
        get => (bool)(this["MinimizarParaBandeja"] ?? true);
        set => this["MinimizarParaBandeja"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("")]
    public string? ChaveIA
    {
        get => this["ChaveIA"] as string;
        set => this["ChaveIA"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("/")]
    public string GatilhoPrefixo
    {
        get => (string)(this["GatilhoPrefixo"] ?? "/");
        set => this["GatilhoPrefixo"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("False")]
    public bool BackupAutomatico
    {
        get => (bool)(this["BackupAutomatico"] ?? false);
        set => this["BackupAutomatico"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("")]
    public string? PastaBackup
    {
        get => this["PastaBackup"] as string;
        set => this["PastaBackup"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("")]
    public string? AppsModoDigitacao
    {
        get => this["AppsModoDigitacao"] as string;
        set => this["AppsModoDigitacao"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("")]
    public string? ApiServidorUrl
    {
        get => this["ApiServidorUrl"] as string;
        set => this["ApiServidorUrl"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("True")]
    public bool SugestaoProativaIA
    {
        get => (bool)(this["SugestaoProativaIA"] ?? true);
        set => this["SugestaoProativaIA"] = value;
    }

    [UserScopedSetting]
    public DateTime UltimoArquivamentoLogs
    {
        get => this["UltimoArquivamentoLogs"] is DateTime d ? d : default;
        set => this["UltimoArquivamentoLogs"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("#6C5CE7")]
    public string CorAccent
    {
        get => (string)(this["CorAccent"] ?? "#6C5CE7");
        set => this["CorAccent"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("pt-BR")]
    public string IdiomaInterface
    {
        get => (string)(this["IdiomaInterface"] ?? "pt-BR");
        set => this["IdiomaInterface"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("False")]
    public bool TourConcluido
    {
        get => (bool)(this["TourConcluido"] ?? false);
        set => this["TourConcluido"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("False")]
    public bool DitadoModoContinuo
    {
        get => (bool)(this["DitadoModoContinuo"] ?? false);
        set => this["DitadoModoContinuo"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("2")]
    public int AtalhoBuscaModificador
    {
        get => (int)(this["AtalhoBuscaModificador"] ?? 2);
        set => this["AtalhoBuscaModificador"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("32")]
    public int AtalhoBuscaTecla
    {
        get => (int)(this["AtalhoBuscaTecla"] ?? 32);
        set => this["AtalhoBuscaTecla"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("48")]
    public int AtalhoRepetirVk
    {
        get => (int)(this["AtalhoRepetirVk"] ?? 48);
        set => this["AtalhoRepetirVk"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("86")]
    public int AtalhoDitadoVk
    {
        get => (int)(this["AtalhoDitadoVk"] ?? 86);
        set => this["AtalhoDitadoVk"] = value;
    }

    [UserScopedSetting, DefaultSettingValue("False")]
    public bool ModoKiosk
    {
        get => (bool)(this["ModoKiosk"] ?? false);
        set => this["ModoKiosk"] = value;
    }
}
