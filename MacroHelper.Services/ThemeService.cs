#if WINDOWS
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
#endif

namespace MacroHelper.Services;

public enum TemaApp { Sistema, Claro, Escuro }

public class ThemeService
{
    public TemaApp TemaAtual { get; private set; } = TemaApp.Sistema;
    public Func<string>?   LerPreferencia    { get; set; }
    public Action<string>? SalvarPreferencia { get; set; }

    public void AplicarTema(TemaApp tema)
    {
        TemaAtual = tema;
#if WINDOWS
        var isDark = tema == TemaApp.Escuro || (tema == TemaApp.Sistema && IsSystemDark());
        AplicarRecursos(isDark);
#endif
        SalvarPreferencia?.Invoke(tema.ToString());
    }

    public void AplicarCorAccent(string hexCor)
    {
#if WINDOWS
        try
        {
            var app = Application.Current;
            if (app == null) return;
            var cor = (Color)ColorConverter.ConvertFromString(hexCor);
            app.Resources["AccentBrush"]      = new SolidColorBrush(cor);
            app.Resources["AccentHoverBrush"] = new SolidColorBrush(AjustarBrilho(cor, -20));
            app.Resources["AccentTextBrush"]  = new SolidColorBrush(cor);
            app.Resources["AccentLightBrush"] = new SolidColorBrush(Color.FromArgb(30, cor.R, cor.G, cor.B));
        }
        catch { /* cor inválida — ignora */ }
#endif
    }

#if WINDOWS
    private static Color AjustarBrilho(Color c, int delta)
    {
        byte Ajustar(byte v) => (byte)Math.Clamp(v + delta, 0, 255);
        return Color.FromRgb(Ajustar(c.R), Ajustar(c.G), Ajustar(c.B));
    }
#endif

    public void CarregarPreferencia()
    {
        var salvo = LerPreferencia?.Invoke() ?? "Sistema";
        var tema  = salvo switch
        {
            "Claro"  => TemaApp.Claro,
            "Escuro" => TemaApp.Escuro,
            _        => TemaApp.Sistema
        };
        AplicarTema(tema);
    }

#if WINDOWS
    private static void AplicarRecursos(bool isDark)
    {
        var app = Application.Current;
        if (app == null) return;
        var uri = new Uri(isDark
            ? "/MacroHelper;component/Themes/Dark.xaml"
            : "/MacroHelper;component/Themes/Light.xaml",
            UriKind.RelativeOrAbsolute);
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("/Themes/") == true);
        if (existing != null) app.Resources.MergedDictionaries.Remove(existing);
        app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = uri });
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return false; }
    }
#endif
}
