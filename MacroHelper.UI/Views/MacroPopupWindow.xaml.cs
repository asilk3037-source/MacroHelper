using MacroHelper.Core.Entities;
using MacroHelper.Services;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MacroHelper.UI.Views;

public partial class MacroPopupWindow : Window
{
    private readonly TextInsertionService _insertionService;
    private readonly UsuarioService       _usuarioService;
    private readonly MacroService         _macroService;
    private string _gatilhoAtual = string.Empty;

    public event Action<Macro>? MacroInserida;

    public MacroPopupWindow(TextInsertionService insertionService, UsuarioService usuarioService, MacroService macroService)
    {
        InitializeComponent();
        _insertionService = insertionService;
        _usuarioService   = usuarioService;
        _macroService     = macroService;
    }

    public void AtualizarSugestoes(string gatilho, IEnumerable<Macro> macros)
    {
        _gatilhoAtual = gatilho;
        TxtGatilho.Text = gatilho;
        ListMacros.ItemsSource = macros.ToList();
        if (ListMacros.Items.Count > 0) ListMacros.SelectedIndex = 0;
        PositionarNoCursor();
        if (!IsVisible) Show();
    }

    public void Fechar() => Hide();

    private void PositionarNoCursor()
    {
        GetCursorPos(out var pt);
        double left = pt.X + 4;
        double top  = pt.Y + 24;
        if (left + 380 > SystemParameters.VirtualScreenWidth)  left = pt.X - 384;
        if (top  + 320 > SystemParameters.VirtualScreenHeight) top  = pt.Y - 320;
        Left = left; Top = top;
    }

    private void ListMacros_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Return or Key.Enter) { InserirSelecionada(); e.Handled = true; }
        else if (e.Key == Key.Escape)         { Fechar(); e.Handled = true; }
    }

    private void ListMacros_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ListMacros.SelectedItem is Macro) InserirSelecionada();
    }

    private async void InserirSelecionada()
    {
        if (ListMacros.SelectedItem is not Macro macro) return;
        Hide();

        var conteudoBase = await _macroService.ResolverMacrosAninhadasAsync(macro.Conteudo);
        conteudoBase = _macroService.ResolverCondicionais(conteudoBase, macro);

        // Verifica se tem variáveis dinâmicas
        if (VariavelService.TemVariaveis(conteudoBase))
        {
            var nomeUsuario = _usuarioService.UsuarioAtual?.Nome ?? string.Empty;
            var variaveis   = VariavelService.ExtrairVariaveis(conteudoBase, nomeUsuario);

            // Se TODAS são auto-preencher, não mostra janela
            if (variaveis.All(v => v.AutoPreencher))
            {
                var valores  = variaveis.ToDictionary(v => v.Nome, v => v.ValorPadrao ?? "");
                var conteudo = VariavelService.Substituir(conteudoBase, valores);
                MacroInserida?.Invoke(macro);
                _ = _insertionService.InserirTextoAsync(_gatilhoAtual, conteudo);
                return;
            }

            // Mostra janela para preencher variáveis manuais
            var janela = new VariaveisWindow(conteudoBase, variaveis);
            if (janela.ShowDialog() == true && janela.ConteudoFinal != null)
            {
                MacroInserida?.Invoke(macro);
                _ = _insertionService.InserirTextoAsync(_gatilhoAtual, janela.ConteudoFinal);
            }
            return;
        }

        MacroInserida?.Invoke(macro);
        _ = _insertionService.InserirTextoAsync(_gatilhoAtual, conteudoBase);
    }

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT p);
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
}
