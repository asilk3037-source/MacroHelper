using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Data.Context;
using MacroHelper.Services;
using MacroHelper.UI.Properties;
using System.Windows.Input;
using HotkeyService = MacroHelper.UI.HotkeyService;

namespace MacroHelper.UI.ViewModels;

public partial class ConfiguracoesViewModel : ObservableObject
{
    private readonly ThemeService    _themeService;
    private readonly IaService?      _iaService;
    private readonly DatabaseContext _dbContext;
    private readonly BackupService   _backupService;
    private readonly SyncService     _syncService;
    private readonly UsuarioService  _usuarioService;
    private readonly VoiceDictationService _voiceService;
    private readonly MacroService    _macroService;
    private readonly HealthService   _healthService;
    private readonly HotkeyService   _hotkeyService;
    private readonly KeyboardHookService _hookService;
    private readonly KioskModeService _kioskService;

    [ObservableProperty] private string _temaSelecionado     = "Sistema";
    [ObservableProperty] private bool   _iniciarComWindows   = false;
    [ObservableProperty] private bool   _minimizarParaBandeja = true;
    [ObservableProperty] private string _mensagem            = string.Empty;
    [ObservableProperty] private bool   _mensagemSucesso     = true;

    // Banco / modo equipe
    [ObservableProperty] private string _caminhoBanco   = DatabaseConfig.ObterCaminhoAtivo();
    [ObservableProperty] private bool   _modoEquipe     = DatabaseConfig.IsRedeLocal();
    [ObservableProperty] private string _statusBanco    = string.Empty;

    // IA
    [ObservableProperty] private string _chaveIA        = string.Empty;
    [ObservableProperty] private bool   _iaConfigurada  = false;

    // Gatilho / digitação
    [ObservableProperty] private string  _gatilhoPrefixo      = "/";
    [ObservableProperty] private string  _appsModoDigitacao   = string.Empty;
    [ObservableProperty] private bool    _sugestaoProativaIA  = true;

    // Backup
    [ObservableProperty] private bool    _backupAutomatico = false;
    [ObservableProperty] private string  _pastaBackup      = string.Empty;
    [ObservableProperty] private bool    _fazendoBackup    = false;

    // Sync (modo equipe via API)
    [ObservableProperty] private string  _apiServidorUrl   = string.Empty;
    [ObservableProperty] private string  _syncEmail        = string.Empty;
    [ObservableProperty] private string  _syncSenha        = string.Empty;
    [ObservableProperty] private bool    _sincronizando     = false;
    [ObservableProperty] private bool?   _servidorOnline    = null;
    [ObservableProperty] private bool    _verificandoServidor = false;

    // Aparência avançada / idioma
    [ObservableProperty] private string  _corAccentSelecionada = "#6C5CE7";
    [ObservableProperty] private string  _idiomaSelecionado    = "pt-BR";
    public List<string> CoresAccentDisponiveis { get; } =
        ["#6C5CE7", "#00B894", "#E17055", "#0984E3", "#E84393", "#FDCB6E", "#D63031", "#636E72"];

    // Ditado por voz
    [ObservableProperty] private bool _ditadoModoContinuo = false;

    // Saúde do app
    [ObservableProperty] private string  _tamanhoBancoTexto   = "—";
    [ObservableProperty] private string  _ultimoBackupTexto   = "—";
    [ObservableProperty] private int     _totalMacrosHealth   = 0;
    [ObservableProperty] private int     _totalUsuariosHealth = 0;
    [ObservableProperty] private string  _memoriaTexto        = "—";
    [ObservableProperty] private string  _versaoAppTexto      = "—";
    [ObservableProperty] private bool    _bancoIntegro        = true;
    [ObservableProperty] private bool    _carregandoSaude     = false;

    // Recursos da máquina local (do usuário logado nesta sessão)
    [ObservableProperty] private string  _cpuTexto            = "—";
    [ObservableProperty] private string  _memoriaSistemaTexto = "—";
    [ObservableProperty] private string  _discoTexto          = "—";

    // Atalhos de teclado remapeáveis
    [ObservableProperty] private string  _atalhoBuscaTexto    = string.Empty;
    [ObservableProperty] private string  _atalhoRepetirTexto  = string.Empty;
    [ObservableProperty] private string  _atalhoDitadoTexto   = string.Empty;
    [ObservableProperty] private string? _capturandoAtalho    = null;

    // Modo totem/kiosk
    [ObservableProperty] private bool _modoKiosk = false;

    public ConfiguracoesViewModel(ThemeService themeService, IaService? iaService,
        DatabaseContext dbContext, BackupService backupService, SyncService syncService,
        UsuarioService usuarioService, VoiceDictationService voiceService, MacroService macroService,
        HealthService healthService, HotkeyService hotkeyService, KeyboardHookService hookService,
        KioskModeService kioskService)
    {
        _healthService   = healthService;
        _hotkeyService   = hotkeyService;
        _hookService      = hookService;
        _kioskService     = kioskService;
        _themeService    = themeService;
        _iaService       = iaService;
        _dbContext       = dbContext;
        _backupService   = backupService;
        _syncService     = syncService;
        _usuarioService  = usuarioService;
        _voiceService    = voiceService;
        _macroService    = macroService;
        TemaSelecionado = themeService.TemaAtual.ToString();

        try
        {
            IniciarComWindows    = Settings.Default.IniciarComWindows;
            MinimizarParaBandeja = Settings.Default.MinimizarParaBandeja;
            ChaveIA              = Settings.Default.ChaveIA ?? string.Empty;
            IaConfigurada        = !string.IsNullOrEmpty(ChaveIA);

            GatilhoPrefixo     = string.IsNullOrEmpty(Settings.Default.GatilhoPrefixo) ? "/" : Settings.Default.GatilhoPrefixo;
            AppsModoDigitacao  = Settings.Default.AppsModoDigitacao ?? string.Empty;
            SugestaoProativaIA = Settings.Default.SugestaoProativaIA;

            BackupAutomatico = Settings.Default.BackupAutomatico;
            PastaBackup      = string.IsNullOrEmpty(Settings.Default.PastaBackup) ? _backupService.PastaBackup : Settings.Default.PastaBackup;

            ApiServidorUrl = Settings.Default.ApiServidorUrl ?? string.Empty;

            CorAccentSelecionada = string.IsNullOrEmpty(Settings.Default.CorAccent) ? "#6C5CE7" : Settings.Default.CorAccent;
            IdiomaSelecionado    = string.IsNullOrEmpty(Settings.Default.IdiomaInterface) ? "pt-BR" : Settings.Default.IdiomaInterface;
            DitadoModoContinuo   = Settings.Default.DitadoModoContinuo;
        }
        catch { }

        ModoKiosk = _kioskService.Ativo;

        AtualizarStatusBanco();
        AtualizarTextosAtalhos();
        _ = CarregarSaudeAsync();
    }

    [RelayCommand]
    public void AlternarModoKiosk()
    {
        ModoKiosk = !ModoKiosk;
        _kioskService.Definir(ModoKiosk);
        Settings.Default.ModoKiosk = ModoKiosk;
        Settings.Default.Save();
        MostrarMsg(ModoKiosk
            ? "Modo totem ativado — a barra lateral foi ocultada."
            : "Modo totem desativado.", true);
    }

    private void AtualizarStatusBanco()
    {
        var caminho = DatabaseConfig.ObterCaminhoAtivo();
        StatusBanco = DatabaseConfig.IsRedeLocal()
            ? $"Modo equipe: {caminho}"
            : $"Modo local: {caminho}";
    }

    // ── Atalhos de teclado remapeáveis ─────────────────────────
    private void AtualizarTextosAtalhos()
    {
        AtalhoBuscaTexto   = FormatarComboCompleto((ModifierKeys)Settings.Default.AtalhoBuscaModificador, Settings.Default.AtalhoBuscaTecla);
        AtalhoRepetirTexto = $"Ctrl + Alt + {FormatarTecla(Settings.Default.AtalhoRepetirVk)}";
        AtalhoDitadoTexto  = $"Ctrl + Alt + {FormatarTecla(Settings.Default.AtalhoDitadoVk)}";
    }

    private static string FormatarComboCompleto(ModifierKeys mods, int vk)
    {
        var partes = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) partes.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt))     partes.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift))   partes.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Windows)) partes.Add("Win");
        partes.Add(FormatarTecla(vk));
        return string.Join(" + ", partes);
    }

    private static string FormatarTecla(int vk)
    {
        try
        {
            var key = KeyInterop.KeyFromVirtualKey(vk);
            return key switch
            {
                Key.Space => "Espaço",
                Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
                Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
                _ => key.ToString().ToUpperInvariant()
            };
        }
        catch { return "?"; }
    }

    [RelayCommand]
    public void IniciarCapturaAtalho(string qual) => CapturandoAtalho = qual;

    [RelayCommand]
    public void CancelarCapturaAtalho() => CapturandoAtalho = null;

    /// <summary>Chamado pelo code-behind da View quando uma tecla é pressionada durante a captura de um atalho.</summary>
    public void FinalizarCapturaAtalho(int vk, ModifierKeys modificadores)
    {
        var qual = CapturandoAtalho;
        CapturandoAtalho = null;
        if (qual == null) return;

        try
        {
            switch (qual)
            {
                case "busca":
                    if (modificadores == ModifierKeys.None)
                    {
                        MostrarMsg("Escolha uma combinação com Ctrl, Alt, Shift ou Win.", false);
                        return;
                    }
                    if (!_hotkeyService.Reconfigurar((int)modificadores, vk))
                    {
                        MostrarMsg("Essa combinação já está em uso por outro programa. Escolha outra.", false);
                        return;
                    }
                    Settings.Default.AtalhoBuscaModificador = (int)modificadores;
                    Settings.Default.AtalhoBuscaTecla       = vk;
                    break;

                case "repetir":
                    _hookService.VkRepetirUltima = vk;
                    Settings.Default.AtalhoRepetirVk = vk;
                    break;

                case "ditado":
                    _hookService.VkDitado = vk;
                    Settings.Default.AtalhoDitadoVk = vk;
                    break;
            }

            Settings.Default.Save();
            AtualizarTextosAtalhos();
            MostrarMsg("Atalho atualizado.", true);
        }
        catch (Exception ex) { MostrarMsg($"Erro: {ex.Message}", false); }
    }

    [RelayCommand]
    public void RestaurarAtalhosPadrao()
    {
        Settings.Default.AtalhoBuscaModificador = 2;
        Settings.Default.AtalhoBuscaTecla       = 0x20;
        Settings.Default.AtalhoRepetirVk        = 0x30;
        Settings.Default.AtalhoDitadoVk         = 0x56;
        Settings.Default.Save();

        _hotkeyService.Reconfigurar(2, 0x20);
        _hookService.VkRepetirUltima = 0x30;
        _hookService.VkDitado        = 0x56;

        AtualizarTextosAtalhos();
        MostrarMsg("Atalhos restaurados ao padrão.", true);
    }

    [RelayCommand]
    public async Task CarregarSaudeAsync()
    {
        CarregandoSaude = true;
        try
        {
            var info = await _healthService.ObterAsync();
            TamanhoBancoTexto   = FormatarBytes(info.TamanhoBancoBytes);
            UltimoBackupTexto   = info.UltimoBackup?.ToString("dd/MM/yyyy HH:mm") ?? "Nenhum backup ainda";
            TotalMacrosHealth   = info.TotalMacros;
            TotalUsuariosHealth = info.TotalUsuarios;
            MemoriaTexto        = FormatarBytes(info.MemoriaProcessoBytes);
            VersaoAppTexto      = info.VersaoApp;
            BancoIntegro        = info.BancoIntegro;

            CpuTexto            = $"{info.CpuUsoPercent:0.#}%";
            MemoriaSistemaTexto = info.MemoriaSistemaTotalBytes > 0
                ? $"{FormatarBytes(info.MemoriaSistemaUsadaBytes)} / {FormatarBytes(info.MemoriaSistemaTotalBytes)}"
                : "—";
            DiscoTexto          = info.DiscoTotalBytes > 0
                ? $"{FormatarBytes(info.DiscoTotalBytes - info.DiscoLivreBytes)} / {FormatarBytes(info.DiscoTotalBytes)}"
                : "—";
        }
        catch (Exception ex) { MostrarMsg($"Erro ao verificar saúde do app: {ex.Message}", false); }
        finally { CarregandoSaude = false; }
    }

    private static string FormatarBytes(long bytes)
    {
        double valor = bytes;
        string[] unidades = ["B", "KB", "MB", "GB"];
        var i = 0;
        while (valor >= 1024 && i < unidades.Length - 1) { valor /= 1024; i++; }
        return $"{valor:0.#} {unidades[i]}";
    }

    [RelayCommand]
    public void AplicarTema(string tema)
    {
        TemaSelecionado = tema;
        _themeService.AplicarTema(tema switch
        {
            "Claro"  => TemaApp.Claro,
            "Escuro" => TemaApp.Escuro,
            _        => TemaApp.Sistema
        });
        MostrarMsg("Tema aplicado!", true);
    }

    [RelayCommand]
    public void SalvarBanco()
    {
        if (string.IsNullOrWhiteSpace(CaminhoBanco))
        {
            MostrarMsg("Caminho inválido.", false); return;
        }
        try
        {
            _dbContext.TrocarCaminho(CaminhoBanco);
            AtualizarStatusBanco();
            MostrarMsg("Banco de dados atualizado! Reinicie o app para aplicar.", true);
        }
        catch (Exception ex) { MostrarMsg($"Erro: {ex.Message}", false); }
    }

    [RelayCommand]
    public void UsarBancoLocal()
    {
        CaminhoBanco = DatabaseConfig.CaminhoLocal;
        SalvarBanco();
    }

    [RelayCommand]
    public void SalvarChaveIA()
    {
        if (_iaService != null)
            _iaService.ApiKey = ChaveIA;

        IaConfigurada = !string.IsNullOrEmpty(ChaveIA);

        try
        {
            Settings.Default.ChaveIA = ChaveIA;
            Settings.Default.Save();
        }
        catch { }
        MostrarMsg(IaConfigurada ? "IA configurada com sucesso." : "Chave removida.", true);
    }

    [RelayCommand]
    public void SalvarConfiguracoes()
    {
        try
        {
            Settings.Default.IniciarComWindows    = IniciarComWindows;
            Settings.Default.MinimizarParaBandeja = MinimizarParaBandeja;
            Settings.Default.Save();
            MostrarMsg("Configurações salvas!", true);
        }
        catch { MostrarMsg("Erro ao salvar.", false); }
    }

    [RelayCommand]
    public void SalvarGatilho()
    {
        try
        {
            Settings.Default.GatilhoPrefixo     = string.IsNullOrWhiteSpace(GatilhoPrefixo) ? "/" : GatilhoPrefixo[..1];
            Settings.Default.AppsModoDigitacao  = AppsModoDigitacao;
            Settings.Default.SugestaoProativaIA = SugestaoProativaIA;
            Settings.Default.Save();
            MostrarMsg("Configurações de gatilho salvas! Reinicie o app para aplicar.", true);
        }
        catch { MostrarMsg("Erro ao salvar.", false); }
    }

    [RelayCommand]
    public void SalvarBackupConfig()
    {
        try
        {
            Settings.Default.BackupAutomatico = BackupAutomatico;
            Settings.Default.PastaBackup      = PastaBackup;
            Settings.Default.Save();
            _backupService.Iniciar(BackupAutomatico, PastaBackup);
            MostrarMsg("Configurações de backup salvas!", true);
        }
        catch (Exception ex) { MostrarMsg($"Erro: {ex.Message}", false); }
    }

    [RelayCommand]
    public async Task FazerBackupAgora()
    {
        FazendoBackup = true;
        try
        {
            var (ok, msg) = await _backupService.FazerBackupAgoraAsync();
            MostrarMsg(msg, ok);
        }
        finally { FazendoBackup = false; }
    }

    [RelayCommand]
    public void SalvarServidorSync()
    {
        try
        {
            Settings.Default.ApiServidorUrl = ApiServidorUrl;
            Settings.Default.Save();
            MostrarMsg("Servidor de sincronização salvo!", true);
        }
        catch { MostrarMsg("Erro ao salvar.", false); }
    }

    [RelayCommand]
    public async Task Sincronizar()
    {
        if (string.IsNullOrWhiteSpace(SyncEmail) || string.IsNullOrWhiteSpace(SyncSenha))
        {
            MostrarMsg("Informe e-mail e senha de sincronização.", false); return;
        }
        Sincronizando = true;
        try
        {
            var (ok, msg) = await _syncService.SincronizarAsync(ApiServidorUrl, SyncEmail, SyncSenha);
            MostrarMsg(msg, ok);
        }
        finally { Sincronizando = false; }
    }

    [RelayCommand]
    public async Task VerificarServidor()
    {
        VerificandoServidor = true;
        try
        {
            var (ok, msg) = await _syncService.VerificarServidorAsync(ApiServidorUrl);
            ServidorOnline = ok;
            MostrarMsg(msg, ok);
        }
        finally { VerificandoServidor = false; }
    }

    [RelayCommand]
    public void AplicarCorAccent(string cor)
    {
        CorAccentSelecionada = cor;
        try
        {
            Settings.Default.CorAccent = cor;
            Settings.Default.Save();
            _themeService.AplicarCorAccent(cor);
            MostrarMsg("Cor de destaque aplicada!", true);
        }
        catch (Exception ex) { MostrarMsg($"Erro: {ex.Message}", false); }
    }

    [RelayCommand]
    public void AplicarIdioma(string idioma)
    {
        IdiomaSelecionado = idioma;
        try
        {
            Settings.Default.IdiomaInterface = idioma;
            Settings.Default.Save();
            MostrarMsg("Idioma salvo! Reinicie o app para aplicar.", true);
        }
        catch { MostrarMsg("Erro ao salvar.", false); }
    }

    [RelayCommand]
    public void RefazerTour()
    {
        try
        {
            Settings.Default.TourConcluido = false;
            Settings.Default.Save();
            MostrarMsg("O tour será exibido na próxima abertura do app.", true);
        }
        catch { MostrarMsg("Erro ao salvar.", false); }
    }

    [RelayCommand]
    public async Task AlternarDitadoModoContinuoAsync()
    {
        var ativar = !DitadoModoContinuo;
        try
        {
            if (ativar)
            {
                var atalhos = (await _macroService.ObterTodosAsync()).Select(m => m.Atalho);
                _voiceService.AtualizarVocabulario(atalhos);
                _voiceService.IniciarModoContinuo();
            }
            else
            {
                _voiceService.PararModoContinuo();
            }

            DitadoModoContinuo = ativar;
            Settings.Default.DitadoModoContinuo = ativar;
            Settings.Default.Save();
            MostrarMsg(ativar
                ? "Modo contínuo ativo — diga \"ei macro, inserir <atalho>\" quando quiser."
                : "Modo contínuo desativado.", true);
        }
        catch (Exception ex) { MostrarMsg($"Erro: {ex.Message}", false); }
    }

    private void MostrarMsg(string msg, bool ok)
    {
        Mensagem = msg; MensagemSucesso = ok;
        if (System.Threading.SynchronizationContext.Current != null)
            Task.Delay(3500).ContinueWith(_ => Mensagem = string.Empty,
                TaskScheduler.FromCurrentSynchronizationContext());
    }
}
