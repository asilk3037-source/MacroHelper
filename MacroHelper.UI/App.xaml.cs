using MacroHelper.Data.Context;
using MacroHelper.Data.Repositories;
using MacroHelper.Core.Interfaces;
using MacroHelper.Services;
using MacroHelper.UI.Properties;
using MacroHelper.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Windows;
using Application = System.Windows.Application;

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

    // Ativa explicitamente o layout PT-BR em qualquer janela que ganhar foco
    public static void RestaurarIdiomaDoSistema()
    {
        if (_ptBrLayout != IntPtr.Zero)
            ActivateKeyboardLayout(_ptBrLayout, 0);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

    /// <summary>Tenta logar automaticamente com a sessão do Supabase Auth salva de um login anterior.</summary>
    private static async Task<bool> TentarRestaurarSessaoAsync()
    {
        var sessao = SupabaseSessionStore.Load();
        if (sessao == null) return false;

        try
        {
            var supabaseCtx = Services.GetRequiredService<SupabaseContext>();
            await supabaseCtx.Auth.SetSession(sessao.Value.AccessToken, sessao.Value.RefreshToken);

            var authUserId = supabaseCtx.Auth.CurrentSession?.User?.Id;
            if (authUserId == null) { SupabaseSessionStore.Clear(); return false; }

            var usuarioRepo = Services.GetRequiredService<UsuarioRepository>();
            var usuario = await usuarioRepo.ObterPorAuthUserIdAsync(Guid.Parse(authUserId));
            if (usuario == null || !usuario.Ativo) { SupabaseSessionStore.Clear(); return false; }

            // O refresh token do Supabase é rotativo (uso único) — SetSession pode ter emitido
            // um novo par de tokens. Salva de novo para a PRÓXIMA reabertura não usar um
            // refresh token já consumido.
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
        s.AddSingleton<WebhookRepository>();
        s.AddSingleton<NotificacaoRepository>();

        // Services
        s.AddSingleton<WebhookService>();
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
        s.AddTransient<ViewModels.IntegracoesViewModel>();
        s.AddTransient<ViewModels.NotificacoesViewModel>();
        s.AddTransient<ViewModels.PerfilViewModel>();
        s.AddTransient<ViewModels.AjudaViewModel>();

        // Views
        s.AddTransient<LoginWindow>();
        s.AddTransient<MainWindow>();
    }
}
