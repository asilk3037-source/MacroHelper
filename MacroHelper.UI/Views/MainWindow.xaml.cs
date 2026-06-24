using MacroHelper.Services;
using MacroHelper.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Application = System.Windows.Application;

namespace MacroHelper.UI.Views;

public partial class MainWindow : Window
{
    private readonly KeyboardHookService  _hookService;
    private readonly MacroService         _macroService;
    private readonly TextInsertionService _insertionService;
    private readonly LogUsoService        _logService;
    private readonly UsuarioService       _usuarioService;
    private readonly IaService            _iaService;
    private readonly TrayService          _trayService;
    private readonly HotkeyService        _hotkeyService;
    private readonly VoiceDictationService _voiceService;
    private readonly BackupService        _backupService;
    private MacroPopupWindow?             _popup;
    private BuscadorRapidoWindow?         _buscador;
    private bool                          _hookAtivo = true;

    public MainWindow()
    {
        InitializeComponent();
        DataContext       = App.Services.GetRequiredService<MainViewModel>();
        _hookService      = App.Services.GetRequiredService<KeyboardHookService>();
        _macroService     = App.Services.GetRequiredService<MacroService>();
        _insertionService = App.Services.GetRequiredService<TextInsertionService>();
        _logService       = App.Services.GetRequiredService<LogUsoService>();
        _usuarioService   = App.Services.GetRequiredService<UsuarioService>();
        _iaService        = App.Services.GetRequiredService<IaService>();
        _voiceService     = App.Services.GetRequiredService<VoiceDictationService>();
        _backupService    = App.Services.GetRequiredService<BackupService>();

        _trayService   = new TrayService();
        _hotkeyService = App.Services.GetRequiredService<HotkeyService>();

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
        StateChanged += OnStateChanged;
        Activated += (_, _) => App.RestaurarIdiomaDoSistema();

        if (DataContext is MainViewModel mvm)
        {
            mvm.ModoKioskAlterado += (_, ativo) =>
                Dispatcher.Invoke(() => WindowState = ativo ? WindowState.Maximized : WindowState.Normal);
            if (mvm.ModoKiosk) WindowState = WindowState.Maximized;
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        IniciarHook();
        IniciarTray();
        IniciarHotkey();
        MostrarTourSeNecessario();
    }

    private void MostrarTourSeNecessario()
    {
        try
        {
            if (Properties.Settings.Default.TourConcluido) return;
            var tour = new TourWindow();
            tour.Closed += (_, _) =>
            {
                Properties.Settings.Default.TourConcluido = true;
                Properties.Settings.Default.Save();
            };
            tour.Show();
        }
        catch { }
    }

    // ── Hook de teclado ─────────────────────────────────────────
    private string? _ultimaFraseSugerida;

    private void IniciarHook()
    {
        // Prefixo de gatilho customizável
        var prefixoCfg = Properties.Settings.Default.GatilhoPrefixo;
        _hookService.Prefixo = !string.IsNullOrEmpty(prefixoCfg) ? prefixoCfg[0] : '/';

        // Apps que devem usar digitação direta em vez de colar
        var appsDigitacao = Properties.Settings.Default.AppsModoDigitacao;
        if (!string.IsNullOrWhiteSpace(appsDigitacao))
            _insertionService.AppsModoDigitacao = appsDigitacao
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        _hookService.DeteccaoFraseAtiva = Properties.Settings.Default.SugestaoProativaIA;

        // Tecla final de Ctrl+Alt+<tecla> para repetir última macro / alternar ditado (remapeável em Configurações)
        _hookService.VkRepetirUltima = Properties.Settings.Default.AtalhoRepetirVk;
        _hookService.VkDitado        = Properties.Settings.Default.AtalhoDitadoVk;

        _popup = new MacroPopupWindow(_insertionService, _usuarioService, _macroService);
        _popup.MacroInserida += async (macro) =>
        {
            await _logService.RegistrarAsync(
                macro.Id, macro.Titulo, macro.Atalho,
                _usuarioService.UsuarioAtual?.Id, macro.Conteudo.Length);

            // Atualiza stats na sidebar
            if (DataContext is MainViewModel vm) vm.TotalMacros++;
        };

        _hookService.GatilhoDetectado += async (_, gatilho) =>
        {
            if (!_hookAtivo) return;
            var macros = await _macroService.BuscarPorGatilhoAsync(gatilho, _hookService.Prefixo);
            var lista  = macros.ToList();
            await Dispatcher.InvokeAsync(() =>
            {
                if (lista.Count > 0) _popup!.AtualizarSugestoes(gatilho, lista);
                else                 _popup!.Fechar();
            });
        };

        _hookService.GatilhoCancelado += (_, _) =>
            Dispatcher.Invoke(() => _popup?.Fechar());

        // Ctrl+Shift+Z — desfaz a última inserção
        _hookService.UndoSolicitado += (_, _) =>
        {
            if (_insertionService.TemUndoDisponivel)
                _ = _insertionService.DesfazerUltimaInsercaoAsync();
        };

        // Ctrl+Alt+1..9 — atalho de teclado dedicado por macro
        _hookService.AtalhoTecladoPressionado += async (_, digito) =>
        {
            if (!_hookAtivo) return;
            var macro = await _macroService.ObterPorAtalhoTeclaAsync(digito);
            if (macro == null) return;

            var conteudo = await _macroService.ResolverMacrosAninhadasAsync(macro.Conteudo);
            conteudo = _macroService.ResolverCondicionais(conteudo, macro);
            if (VariavelService.TemVariaveis(conteudo))
            {
                var nomeUsuario = _usuarioService.UsuarioAtual?.Nome ?? string.Empty;
                var variaveis   = VariavelService.ExtrairVariaveis(conteudo, nomeUsuario);
                var valores     = variaveis.ToDictionary(v => v.Nome, v => v.ValorPadrao ?? "");
                conteudo = VariavelService.Substituir(conteudo, valores);
            }

            await _insertionService.InserirTextoAsync(string.Empty, conteudo);
            await _logService.RegistrarAsync(macro.Id, macro.Titulo, macro.Atalho,
                _usuarioService.UsuarioAtual?.Id, conteudo.Length);
        };

        // Ctrl+Alt+0 — repete a última macro inserida
        _hookService.RepetirUltimaSolicitado += (_, _) =>
        {
            if (_insertionService.TemUltimaParaRepetir)
                _ = _insertionService.RepetirUltimaInsercaoAsync();
        };

        // Ctrl+Tab / Ctrl+Shift+Tab — navega entre placeholders {{campo}} do último texto inserido
        _hookService.ProximoPlaceholderSolicitado += (_, _) =>
        {
            if (_insertionService.TemPlaceholders) _insertionService.SelecionarProximoPlaceholder();
        };
        _hookService.PlaceholderAnteriorSolicitado += (_, _) =>
        {
            if (_insertionService.TemPlaceholders) _insertionService.SelecionarPlaceholderAnterior();
        };

        // Ctrl+Alt+V — ativa/desativa ditado por voz (push-to-talk manual)
        _hookService.DitadoSolicitado += async (_, _) =>
        {
            if (_voiceService.ModoContinuoAtivo) return; // modo contínuo já cobre o ditado
            if (_voiceService.Ativo) { _voiceService.Alternar(); return; }

            var atalhos = (await _macroService.ObterTodosAsync()).Select(m => m.Atalho);
            _voiceService.AtualizarVocabulario(atalhos);
            _voiceService.Alternar();
        };

        _ = IniciarModoContinuoSeNecessarioAsync();

        async Task IniciarModoContinuoSeNecessarioAsync()
        {
            if (!Properties.Settings.Default.DitadoModoContinuo) return;
            var atalhos = (await _macroService.ObterTodosAsync()).Select(m => m.Atalho);
            _voiceService.AtualizarVocabulario(atalhos);
            _voiceService.IniciarModoContinuo();
        }

        // Frase repetida 3x — sugestão proativa de macro
        _hookService.FraseRepetidaDetectada += (_, frase) =>
        {
            _ultimaFraseSugerida = frase;
            Dispatcher.Invoke(() => _trayService.MostrarNotificacao(
                "Que tal criar uma macro?",
                "Você digitou o mesmo texto 3 vezes. Clique aqui para transformar em macro.",
                System.Windows.Forms.ToolTipIcon.Info, 6000));
        };

        _voiceService.AtalhoReconhecido += async (_, atalho) =>
        {
            var macro = await _macroService.ObterPorAtalhoTeclaAsync(atalho) ?? await BuscarPorAtalhoExatoAsync(atalho);
            if (macro == null) return;
            var conteudo = await _macroService.ResolverMacrosAninhadasAsync(macro.Conteudo);
            conteudo = _macroService.ResolverCondicionais(conteudo, macro);
            await Dispatcher.InvokeAsync(() => _insertionService.InserirTextoAsync(string.Empty, conteudo));
            await _logService.RegistrarAsync(macro.Id, macro.Titulo, macro.Atalho, _usuarioService.UsuarioAtual?.Id, conteudo.Length);
        };
        _voiceService.StatusAlterado += (_, msg) => Dispatcher.Invoke(() =>
        {
            if (DataContext is MainViewModel m) m.StatusMensagem = msg;
        });

        // Backup automático (se configurado em Configurações)
        try
        {
            _backupService.Iniciar(Properties.Settings.Default.BackupAutomatico, Properties.Settings.Default.PastaBackup);
        }
        catch { }

        // Arquivamento de logs antigos (uma vez por dia)
        _ = ArquivarLogsAntigosSeNecessarioAsync();

        _hookService.Iniciar();

        if (DataContext is MainViewModel mvm)
            mvm.StatusMensagem = "SK MacroHelper — Hook ativo · Ctrl+Espaço para busca rápida";
    }

    private async Task<MacroHelper.Core.Entities.Macro?> BuscarPorAtalhoExatoAsync(string atalho)
    {
        var resultados = await _macroService.PesquisarAsync(atalho);
        return resultados.FirstOrDefault(m => string.Equals(m.Atalho, atalho, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ArquivarLogsAntigosSeNecessarioAsync()
    {
        try
        {
            var ultimaExecucao = Properties.Settings.Default.UltimoArquivamentoLogs;
            if (ultimaExecucao != default && (DateTime.Now - ultimaExecucao).TotalHours < 24) return;

            var logService = App.Services.GetRequiredService<LogUsoService>();
            await logService.ArquivarAntigosAsync(DateTime.Now.AddMonths(-6));

            Properties.Settings.Default.UltimoArquivamentoLogs = DateTime.Now;
            Properties.Settings.Default.Save();
        }
        catch { }
    }

    private void AbrirSugestaoDeMacro()
    {
        if (string.IsNullOrWhiteSpace(_ultimaFraseSugerida)) return;
        Show(); WindowState = WindowState.Normal; Activate();
        if (DataContext is MainViewModel vm)
        {
            vm.NavigarParaMacrosCommand.Execute(null);
            vm.AbrirNovaMacroComConteudo(_ultimaFraseSugerida);
        }
        _ultimaFraseSugerida = null;
    }

    // ── Bandeja do sistema ───────────────────────────────────────
    private void IniciarTray()
    {
        var nome = _usuarioService.UsuarioAtual?.Nome ?? "Usuário";
        _trayService.Iniciar(nome);

        _trayService.AbrirJanela += () => Dispatcher.Invoke(() =>
        {
            Show(); WindowState = WindowState.Normal; Activate();
        });

        _trayService.AbrirBuscadorRapido += () =>
            Dispatcher.Invoke(AbrirBuscadorRapido);

        _trayService.AlternarHook += () => Dispatcher.Invoke(() =>
        {
            _hookAtivo = !_hookAtivo;
            _trayService.AtualizarStatus(_hookAtivo);
            if (DataContext is MainViewModel vm)
                vm.StatusMensagem = _hookAtivo
                    ? "Hook ativo · Ctrl+Espaço para busca"
                    : "⏸ Hook pausado — clique no ícone para retomar";
        });

        _trayService.Sair += () => Dispatcher.Invoke(FecharApp);
        _trayService.BalaoClicado += () => Dispatcher.Invoke(AbrirSugestaoDeMacro);
    }

    // ── Hotkey Busca rápida (padrão Ctrl+Espaço, remapeável em Configurações) ──
    private void IniciarHotkey()
    {
        _hotkeyService.Iniciar(this,
            Properties.Settings.Default.AtalhoBuscaModificador,
            Properties.Settings.Default.AtalhoBuscaTecla);
        _hotkeyService.BuscadorRapidoSolicitado += () =>
            Dispatcher.Invoke(() =>
            {
                if (_buscador?.IsVisible == true) _buscador.Hide();
                else                              AbrirBuscadorRapido();
            });
    }

    private void AbrirBuscadorRapido()
    {
        if (_buscador == null)
        {
            _buscador = new BuscadorRapidoWindow(_macroService, _insertionService, _usuarioService, _logService);
            _buscador.MacroSelecionada += async (macro, conteudo) =>
                await _logService.RegistrarAsync(
                    macro.Id, macro.Titulo, macro.Atalho,
                    _usuarioService.UsuarioAtual?.Id, conteudo.Length);
        }
        _buscador.Mostrar();
    }

    // ── Minimizar para tray ──────────────────────────────────────
    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            var settings = App.Services.GetService(typeof(Properties.Settings));
            // Verifica preferência
            try
            {
                if (Properties.Settings.Default.MinimizarParaBandeja)
                {
                    Hide();
                    _trayService.MostrarNotificacao(
                        "SK MacroHelper",
                        "Rodando em segundo plano. Clique duplo no ícone para abrir.",
                        System.Windows.Forms.ToolTipIcon.Info, 2000);
                }
            }
            catch { }
        }
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            if (Properties.Settings.Default.MinimizarParaBandeja)
            {
                e.Cancel = true;
                Hide();
                _trayService.MostrarNotificacao("SK MacroHelper",
                    "Minimizado para a bandeja. Clique duplo para abrir.",
                    System.Windows.Forms.ToolTipIcon.Info, 1500);
                return;
            }
        }
        catch { }
        FecharApp();
    }

    private void FecharApp()
    {
        _hookService.Parar();
        _hotkeyService.Dispose();
        _trayService.Dispose();
        _popup?.Close();
        _buscador?.Close();
        Application.Current.Shutdown();
    }

    // ── Title bar controls ───────────────────────────────────────
    private void Window_Activated(object s, EventArgs e) => App.RestaurarIdiomaDoSistema();

    private void TitleBar_MouseLeftButtonDown(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleMax(); else DragMove();
    }
    private void MinimizeButton_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object s, RoutedEventArgs e) => ToggleMax();
    private void ToggleMax() => WindowState = WindowState == WindowState.Maximized
        ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object s, RoutedEventArgs e)
    {
        try
        {
            if (Properties.Settings.Default.MinimizarParaBandeja) { Hide(); return; }
        }
        catch { }
        FecharApp();
    }
}
