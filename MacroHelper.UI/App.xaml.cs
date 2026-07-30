using MacroHelper.Data.Context;
using MacroHelper.Data.Repositories;
using MacroHelper.Core.Interfaces;
using MacroHelper.Services;
using MacroHelper.UI.Properties;
using MacroHelper.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MacroHelper.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    // LANGID PT-BR = 0x0416; KLF_ACTIVATE = 1
    private const uint KLF_ACTIVATE = 1;
    private static IntPtr _ptBrLayout = IntPtr.Zero;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowByCaption(string? lpClassName, string lpWindowName);
    private static IntPtr FindWindowByCaption(string caption) => FindWindowByCaption(null, caption);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Ativa explicitamente o layout PT-BR em qualquer janela que ganhar foco
    public static void RestaurarIdiomaDoSistema()
    {
        if (_ptBrLayout != IntPtr.Zero)
            ActivateKeyboardLayout(_ptBrLayout, 0);
    }

    private static System.Threading.Mutex? _instanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Garante instância única: se já existe uma rodando, traz ao foco e sai
        _instanceMutex = new System.Threading.Mutex(true, "MacroHelper_SK_SingleInstance", out var isNewInstance);
        if (!isNewInstance)
        {
            // Tenta ativar a janela existente via Win32
            var hWnd = FindWindowByCaption("SK MacroHelper");
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, 9); // SW_RESTORE
                SetForegroundWindow(hWnd);
            }
            _instanceMutex.Close();
            Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogErroFatal(args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogErroFatal(args.Exception);
            args.SetObserved();
        };

        // Carrega o layout PT-BR pelo LANGID fixo — independente do que o Windows
        // associou ao app (per-app language). "00000416" = Portuguese (Brazil).
        _ptBrLayout = LoadKeyboardLayout("00000416", KLF_ACTIVATE);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        await Services.GetRequiredService<SupabaseContext>().InitializeAsync();

        // Conecta ThemeService ao Settings
        var theme = Services.GetRequiredService<ThemeService>();
        theme.LerPreferencia    = () => Settings.Default.Tema ?? "Sistema";
        theme.SalvarPreferencia = v => { Settings.Default.Tema = v; Settings.Default.Save(); };
        theme.CarregarPreferencia();
        if (!string.IsNullOrWhiteSpace(Settings.Default.CorAccent))
            theme.AplicarCorAccent(Settings.Default.CorAccent);

        // Conecta IaService à chave salva
        var ia = Services.GetRequiredService<IaService>();
        ia.ApiKey = Settings.Default.ChaveIA ?? string.Empty;

        // Restaura o modo totem/kiosk salvo
        Services.GetRequiredService<KioskModeService>().Definir(Settings.Default.ModoKiosk);

        if (await TentarRestaurarSessaoAsync())
        {
            var main = Services.GetRequiredService<MainWindow>();
            main.Show();
            return;
        }

        // Abre login
        var login = Services.GetRequiredService<LoginWindow>();
        login.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogErroFatal(e.Exception);
        MessageBox.Show(
            "Ocorreu um erro inesperado e a operação foi cancelada.\n\n" +
            "O MacroHelper continuará aberto — tente novamente. Se o problema persistir, reinicie o aplicativo.\n\n" +
            "Os detalhes técnicos foram salvos em um log para análise.",
            "SK MacroHelper",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }

    private static void LogErroFatal(Exception? ex)
    {
        try
        {
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MacroHelper");
            Directory.CreateDirectory(pasta);
            File.AppendAllText(Path.Combine(pasta, "erros.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Se nem o log puder ser escrito, não há nada mais a fazer aqui.
        }
    }

    /// <summary>Tenta logar automaticamente com a sessão do Supabase Auth salva de um login anterior.</summary>
    private static async Task<bool> TentarRestaurarSessaoAsync()
    {
        var sessao = SupabaseSessionStore.Load();
        if (sessao == null) return false;

        try
        {
            var supabaseCtx = Services.GetRequiredService<SupabaseContext>();
            await supabaseCtx.Auth.SetSession(sessao.Value.AccessToken, sessao.Value.RefreshToken);

            // Força o refresh imediatamente ao restaurar sessão do disco.
            // Isso garante um token fresco E inicia o timer de auto-refresh interno
            // que o SetSession sozinho não ativa.
            try
            {
                await supabaseCtx.Auth.RefreshSession();
            }
            catch { /* Se falhar, continua com o token existente */ }

            var authUserId = supabaseCtx.Auth.CurrentSession?.User?.Id;
            if (authUserId == null) { SupabaseSessionStore.Clear(); return false; }

            var usuarioRepo = Services.GetRequiredService<UsuarioRepository>();
            var usuario = await usuarioRepo.ObterPorAuthUserIdAsync(Guid.Parse(authUserId));
            if (usuario == null || !usuario.Ativo) { SupabaseSessionStore.Clear(); return false; }

            // Salva o par de tokens atualizado pelo RefreshSession
            var sessaoAtual = supabaseCtx.Auth.CurrentSession;
            if (sessaoAtual?.AccessToken != null && sessaoAtual.RefreshToken != null)
                SupabaseSessionStore.Save(sessaoAtual.AccessToken, sessaoAtual.RefreshToken);

            Services.GetRequiredService<UsuarioService>().DefinirUsuarioAtual(usuario);
            return true;
        }
        catch
        {
            SupabaseSessionStore.Clear();
            return false;
        }
    }

    private static void ConfigureServices(IServiceCollection s)
    {
        // Infra
        s.AddSingleton<SupabaseContext>();
        s.AddSingleton<IMacroRepository, MacroRepository>();
        s.AddSingleton<CategoriaRepository>();
        s.AddSingleton<UsuarioRepository>();
        s.AddSingleton<LogUsoRepository>();
        s.AddSingleton<MacroVersaoRepository>();
        s.AddSingleton<LogAuditoriaRepository>();
        s.AddSingleton<GrupoRepository>();
        s.AddSingleton<FavoritoUsuarioRepository>();
        s.AddSingleton<VariavelGlobalRepository>();
        s.AddSingleton<AgendamentoRepository>();
        s.AddSingleton<NotificacaoRepository>();
        s.AddSingleton<PermissaoTelaRepository>();
        s.AddSingleton<IntimacaoErroRepository>();

        // Services
        s.AddSingleton<NotificacaoService>();
        s.AddSingleton<MacroService>();
        s.AddSingleton<CategoriaService>();
        s.AddSingleton<UsuarioService>();
        s.AddSingleton<LogUsoService>();
        s.AddSingleton<ThemeService>();
        s.AddSingleton<TextInsertionService>();
        s.AddSingleton<KeyboardHookService>();
        s.AddSingleton<IaService>();
        s.AddSingleton<VoiceDictationService>();
        s.AddSingleton<GrupoService>();
        s.AddSingleton<HealthService>();
        s.AddSingleton<HotkeyService>();
        s.AddSingleton<KioskModeService>();
        s.AddSingleton<VariavelGlobalService>();
        s.AddSingleton<AgendamentoService>();
        s.AddSingleton<PermissaoTelaService>();
        s.AddSingleton<IntimacoesDwLawService>();
        s.AddSingleton<AtualizadorService>();

        // ViewModels
        s.AddTransient<ViewModels.MainViewModel>();
        s.AddTransient<ViewModels.MacrosViewModel>();
        s.AddTransient<ViewModels.ConfiguracoesViewModel>();
        s.AddTransient<ViewModels.CategoriasViewModel>();
        s.AddTransient<ViewModels.RelatorioViewModel>();
        s.AddTransient<ViewModels.UsuariosViewModel>();
        s.AddTransient<ViewModels.LoginViewModel>();
        s.AddTransient<ViewModels.DashboardViewModel>();
        s.AddTransient<ViewModels.VariaveisGlobaisViewModel>();
        s.AddTransient<ViewModels.HistoricoVersoesViewModel>();
        s.AddTransient<ViewModels.AgendamentosViewModel>();
        s.AddTransient<ViewModels.GruposViewModel>();
        s.AddTransient<ViewModels.AuditoriaViewModel>();
        s.AddTransient<ViewModels.ComunidadeViewModel>();
        s.AddTransient<ViewModels.PermissoesViewModel>();
        s.AddTransient<ViewModels.NotificacoesViewModel>();
        s.AddTransient<ViewModels.PerfilViewModel>();
        s.AddTransient<ViewModels.AjudaViewModel>();
        s.AddTransient<ViewModels.IntimacoesDwLawViewModel>();

        // Views
        s.AddTransient<LoginWindow>();
        s.AddTransient<MainWindow>();
    }
}
