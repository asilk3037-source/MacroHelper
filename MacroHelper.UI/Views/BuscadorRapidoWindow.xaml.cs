using MacroHelper.Core.Entities;
using MacroHelper.Services;
using MacroHelper.UI.Helpers;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MacroHelper.UI.Views;

public partial class BuscadorRapidoWindow : Window
{
    private readonly MacroService         _macroService;
    private readonly TextInsertionService _insertionService;
    private readonly UsuarioService       _usuarioService;
    private readonly LogUsoService        _logService;

    public event Action<Macro, string>? MacroSelecionada;

    public BuscadorRapidoWindow(MacroService macroService,
        TextInsertionService insertionService, UsuarioService usuarioService, LogUsoService logService)
    {
        InitializeComponent();
        _macroService     = macroService;
        _insertionService = insertionService;
        _usuarioService   = usuarioService;
        _logService       = logService;

        Loaded      += (_, _) => { TxtBusca.Focus(); CarregarTodos(); };
        Deactivated += (_, _) => Hide();
        // Restaura o idioma do sistema quando esta janela fica em foco
        Activated   += (_, _) => App.RestaurarIdiomaDoSistema();
        ListResultados.SelectionChanged += (_, _) => AtualizarPreview();
    }

    public void Mostrar()
    {
        TxtBusca.Text = string.Empty;
        CarregarTodos();
        PosicionarNaTelaDoCursor();
        Show();
        Activate();
        TxtBusca.Focus();
    }

    /// <summary>
    /// Centraliza a janela no monitor onde está o cursor do mouse — em vez do monitor
    /// primário (comportamento padrão de WindowStartupLocation="CenterScreen"), que ignora
    /// em qual tela o usuário está trabalhando num setup com múltiplos monitores.
    /// </summary>
    private void PosicionarNaTelaDoCursor()
    {
        if (!GetCursorPos(out var pt)) return;
        var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        if (hMonitor == IntPtr.Zero) return;

        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hMonitor, ref info)) return;

        var dpiX = 96.0;
        var dpiY = 96.0;
        if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out var rawDpiX, out var rawDpiY) == 0)
        {
            dpiX = rawDpiX;
            dpiY = rawDpiY;
        }

        var monitorLeft   = info.rcWork.Left   * 96.0 / dpiX;
        var monitorTop    = info.rcWork.Top    * 96.0 / dpiY;
        var monitorWidth  = (info.rcWork.Right  - info.rcWork.Left) * 96.0 / dpiX;
        var monitorHeight = (info.rcWork.Bottom - info.rcWork.Top)  * 96.0 / dpiY;

        var altura = ActualHeight > 0 ? ActualHeight : Height;
        Left = monitorLeft + (monitorWidth - Width) / 2;
        Top  = monitorTop  + (monitorHeight - altura) / 2;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int  MDT_EFFECTIVE_DPI        = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    private async void CarregarTodos()
    {
        var todos      = (await _macroService.ObterTodosAsync()).ToList();
        var favoritos  = todos.Where(m => m.Favorito).ToList();
        var logsRecentes = (await _logService.ObterRecentesAsync(40)).ToList();
        var idsRecentes  = logsRecentes.Where(l => l.MacroId != null)
            .Select(l => l.MacroId!.Value).Distinct().Take(8).ToList();
        var recentes = idsRecentes
            .Select(id => todos.FirstOrDefault(m => m.Id == id))
            .Where(m => m != null && !favoritos.Any(f => f.Id == m!.Id))
            .Cast<Macro>().ToList();
        var resto = todos
            .Where(m => !favoritos.Any(f => f.Id == m.Id) && !recentes.Any(r => r.Id == m.Id))
            .OrderBy(m => m.Titulo).ToList();

        var lista = new List<Macro>();
        lista.AddRange(favoritos);
        lista.AddRange(recentes);
        lista.AddRange(resto);

        ListResultados.ItemsSource = lista;
        if (ListResultados.Items.Count > 0) ListResultados.SelectedIndex = 0;
    }

    private async void TxtBusca_TextChanged(object sender, TextChangedEventArgs e)
    {
        var termo  = TxtBusca.Text;
        List<Macro> lista;
        if (string.IsNullOrWhiteSpace(termo))
        {
            CarregarTodos();
            return;
        }
        else
        {
            lista = (await _macroService.PesquisarAsync(termo)).ToList();
        }
        ListResultados.ItemsSource = lista;
        if (lista.Count > 0) ListResultados.SelectedIndex = 0;
    }

    private void AtualizarPreview()
    {
        if (ListResultados.SelectedItem is not Macro macro)
        {
            PainelPreview.Visibility = Visibility.Collapsed;
            return;
        }

        var nomeUsuario = _usuarioService.UsuarioAtual?.Nome ?? string.Empty;
        var conteudo = macro.Conteudo;
        if (VariavelService.TemVariaveis(conteudo))
        {
            var variaveis = VariavelService.ExtrairVariaveis(conteudo, nomeUsuario);
            var valores = variaveis.ToDictionary(v => v.Nome, v => v.ValorPadrao ?? $"{{{v.Nome}}}");
            conteudo = VariavelService.Substituir(conteudo, valores);
        }

        PreviewFormatador.Renderizar(TxtPreview, conteudo, (System.Windows.Media.Brush)FindResource("AccentTextBrush"));
        PainelPreview.Visibility = Visibility.Visible;
    }

    private async void BtnFavorito_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: Macro macro }) return;
        await _macroService.ToggleFavoritoAsync(macro);
        e.Handled = true;
        if (string.IsNullOrWhiteSpace(TxtBusca.Text)) CarregarTodos();
    }

    // Captura Up/Down/Enter/Escape em qualquer elemento da janela (PreviewKeyDown = túnel, antes de qualquer controle)
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                MoverSelecao(+1);
                e.Handled = true;
                break;
            case Key.Up:
                MoverSelecao(-1);
                e.Handled = true;
                break;
            case Key.Return:
                InserirSelecionada();
                e.Handled = true;
                break;
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
        }
    }

    private void MoverSelecao(int delta)
    {
        if (ListResultados.Items.Count == 0) return;
        var idx = ListResultados.SelectedIndex + delta;
        idx = Math.Clamp(idx, 0, ListResultados.Items.Count - 1);
        ListResultados.SelectedIndex = idx;
        ListResultados.ScrollIntoView(ListResultados.SelectedItem);
        TxtBusca.Focus(); // mantém foco no campo de texto
    }

    private void TxtBusca_KeyDown(object sender, KeyEventArgs e) { /* tratado em Window_PreviewKeyDown */ }

    private void ListResultados_KeyDown(object sender, KeyEventArgs e) { /* tratado em Window_PreviewKeyDown */ }

    private void ListResultados_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ListResultados.SelectedItem is Macro) InserirSelecionada();
    }

    private async void InserirSelecionada()
    {
        if (ListResultados.SelectedItem is not Macro macro) return;
        Hide();

        var conteudoFinal = await _macroService.ResolverMacrosAninhadasAsync(macro.Conteudo);
        conteudoFinal = _macroService.ResolverCondicionais(conteudoFinal, macro);

        if (VariavelService.TemVariaveis(conteudoFinal))
        {
            var nomeUsuario = _usuarioService.UsuarioAtual?.Nome ?? string.Empty;
            var variaveis   = VariavelService.ExtrairVariaveis(conteudoFinal, nomeUsuario);

            if (!variaveis.All(v => v.AutoPreencher))
            {
                var janela = new VariaveisWindow(conteudoFinal, variaveis);
                if (janela.ShowDialog() != true || janela.ConteudoFinal == null) return;
                conteudoFinal = janela.ConteudoFinal;
            }
            else
            {
                var valores = variaveis.ToDictionary(v => v.Nome, v => v.ValorPadrao ?? "");
                conteudoFinal = VariavelService.Substituir(conteudoFinal, valores);
            }
        }

        MacroSelecionada?.Invoke(macro, conteudoFinal);
        // Cola diretamente sem remover atalho (buscou pelo Ctrl+Espaço)
        _ = _insertionService.InserirTextoAsync(string.Empty, conteudoFinal);
    }
}
