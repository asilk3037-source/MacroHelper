using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MacroHelper.Services;

public class TextInsertionService
{
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr hMem);
    [DllImport("user32.dll")] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr h, StringBuilder t, int c);

    private const byte   VK_BACK           = 0x08;
    private const byte   VK_CONTROL        = 0x11;
    private const byte   VK_SHIFT          = 0x10;
    private const byte   VK_LEFT           = 0x25;
    private const byte   VK_V              = 0x56;
    private const ushort VK_RETURN         = 0x0D;
    private const uint   KEYEVENTF_KEYUP   = 0x0002;
    private const uint   KEYEVENTF_UNICODE = 0x0004;
    private const uint   CF_UNICODETEXT    = 13;
    private const uint   GMEM_MOVEABLE     = 0x0002;
    private const uint   INPUT_KEYBOARD    = 1;

    private static readonly Regex _placeholderRegex = new(@"\{\{[^}]*\}\}", RegexOptions.Compiled);

    // Estado para desfazer a última inserção (Ctrl+Shift+Z)
    private string? _ultimoConteudoInserido;
    private string? _ultimoGatilhoRemovido;
    public bool TemUndoDisponivel => _ultimoConteudoInserido != null;

    // Última macro inserida (independente do undo) — usada para repetir com Ctrl+Alt+0
    private string? _ultimoConteudoParaRepetir;
    public bool TemUltimaParaRepetir => _ultimoConteudoParaRepetir != null;

    // Estado para navegação entre placeholders {{campo}} (Ctrl+Tab / Ctrl+Shift+Tab)
    private List<(int Start, int Length)> _placeholders = new();
    private int  _placeholderIndex = -1;
    private int  _caretPos;
    public bool TemPlaceholders => _placeholders.Count > 0;

    // Apps (substring do título da janela) que devem usar digitação em vez de colar
    public List<string> AppsModoDigitacao { get; set; } = new();

    public async Task InserirTextoAsync(string textoParaRemover, string conteudo, bool forcarDigitacao = false)
    {
        await Task.Delay(60);

        for (int i = 0; i < textoParaRemover.Length; i++)
        {
            keybd_event(VK_BACK, 0, 0, UIntPtr.Zero);
            keybd_event(VK_BACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(12);
        }

        await Task.Delay(40);

        var usarDigitacao = forcarDigitacao || AppAtualPrefereDigitacao();

        if (usarDigitacao)
        {
            DigitarTexto(conteudo);
        }
        else if (!SetClipboardWin32(conteudo))
        {
            // Clipboard indisponível — cai para digitação direta como último recurso
            DigitarTexto(conteudo);
        }
        else
        {
            await Task.Delay(60);
            try
            {
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V,       0, 0, UIntPtr.Zero);
                keybd_event(VK_V,       0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            finally
            {
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        _ultimoConteudoInserido    = conteudo;
        _ultimoGatilhoRemovido     = textoParaRemover;
        _ultimoConteudoParaRepetir = conteudo;

        PrepararPlaceholders(conteudo);
    }

    /// <summary>Reinsere o último conteúdo de macro inserido (Ctrl+Alt+0), sem remover nenhum gatilho.</summary>
    public async Task RepetirUltimaInsercaoAsync()
    {
        if (_ultimoConteudoParaRepetir == null) return;
        await InserirTextoAsync(string.Empty, _ultimoConteudoParaRepetir);
    }

    private void PrepararPlaceholders(string conteudo)
    {
        _placeholders = _placeholderRegex.Matches(conteudo)
            .Select(m => (m.Index, m.Length))
            .ToList();
        _placeholderIndex = -1;
        _caretPos = conteudo.Length;

        if (_placeholders.Count > 0)
            SelecionarProximoPlaceholder();
    }

    /// <summary>
    /// Seleciona o próximo placeholder {{campo}} a partir do cursor atual (estimado).
    /// Funciona de forma confiável enquanto o texto digitado para substituir um placeholder
    /// não mudar o comprimento total — é uma limitação inerente por não termos acesso ao
    /// buffer de texto do app de destino (hook global).
    /// </summary>
    public void SelecionarProximoPlaceholder()
    {
        if (_placeholders.Count == 0) return;
        _placeholderIndex = (_placeholderIndex + 1) % _placeholders.Count;
        MoverESelecionar(_placeholders[_placeholderIndex]);
    }

    public void SelecionarPlaceholderAnterior()
    {
        if (_placeholders.Count == 0) return;
        _placeholderIndex = (_placeholderIndex - 1 + _placeholders.Count) % _placeholders.Count;
        MoverESelecionar(_placeholders[_placeholderIndex]);
    }

    private void MoverESelecionar((int Start, int Length) placeholder)
    {
        var end = placeholder.Start + placeholder.Length;
        var leftPresses = _caretPos - end;

        for (int i = 0; i < Math.Abs(leftPresses); i++)
            EnviarTeclaVirtual(VK_LEFT);

        keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero);
        try
        {
            for (int i = 0; i < placeholder.Length; i++)
                EnviarTeclaVirtual(VK_LEFT);
        }
        finally
        {
            keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        _caretPos = end;
    }

    /// <summary>Desfaz a última macro inserida: remove o texto colado e devolve o gatilho original digitado.</summary>
    public async Task DesfazerUltimaInsercaoAsync()
    {
        if (_ultimoConteudoInserido == null) return;

        var conteudo = _ultimoConteudoInserido;
        var gatilho  = _ultimoGatilhoRemovido ?? string.Empty;
        _ultimoConteudoInserido = null;
        _ultimoGatilhoRemovido  = null;

        await Task.Delay(40);
        for (int i = 0; i < conteudo.Length; i++)
        {
            keybd_event(VK_BACK, 0, 0, UIntPtr.Zero);
            keybd_event(VK_BACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(4);
        }

        if (!string.IsNullOrEmpty(gatilho))
        {
            await Task.Delay(30);
            DigitarTexto(gatilho);
        }
    }

    private bool AppAtualPrefereDigitacao()
    {
        if (AppsModoDigitacao.Count == 0) return false;
        try
        {
            var hwnd = GetForegroundWindow();
            var buf  = new StringBuilder(256);
            GetWindowText(hwnd, buf, 256);
            var titulo = buf.ToString();
            return AppsModoDigitacao.Any(app =>
                !string.IsNullOrWhiteSpace(app) &&
                titulo.Contains(app, StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    /// <summary>Digita texto caractere a caractere via SendInput (Unicode), sem depender da área de transferência.</summary>
    private static void DigitarTexto(string texto)
    {
        foreach (var ch in texto)
        {
            if (ch == '\r') continue;

            if (ch == '\n')
            {
                EnviarTeclaVirtual(VK_RETURN);
                continue;
            }

            EnviarCharUnicode(ch);
        }
    }

    private static void EnviarCharUnicode(char c)
    {
        var down = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = KEYEVENTF_UNICODE, time = 0, dwExtraInfo = IntPtr.Zero } }
        };
        var up = down;
        up.U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
        SendInput(2, new[] { down, up }, Marshal.SizeOf<INPUT>());
    }

    private static void EnviarTeclaVirtual(ushort vk)
    {
        var down = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = 0, time = 0, dwExtraInfo = IntPtr.Zero } } };
        var up = down;
        up.U.ki.dwFlags = KEYEVENTF_KEYUP;
        SendInput(2, new[] { down, up }, Marshal.SizeOf<INPUT>());
    }

    private static bool SetClipboardWin32(string text)
    {
        // Retry até 5x — OpenClipboard falha se outro processo tem o clipboard aberto
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero)) break;
            Thread.Sleep(20);
            if (attempt == 4) return false;
        }

        var hGlobal = IntPtr.Zero;
        try
        {
            EmptyClipboard();

            var bytes = (text.Length + 1) * 2;
            hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
            if (hGlobal == IntPtr.Zero) return false;

            var ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                hGlobal = IntPtr.Zero;
                return false;
            }

            Marshal.Copy(text.ToCharArray(), 0, ptr, text.Length);
            Marshal.WriteInt16(ptr, text.Length * 2, 0);
            GlobalUnlock(hGlobal);

            var result = SetClipboardData(CF_UNICODETEXT, hGlobal);
            if (result != IntPtr.Zero)
            {
                // SetClipboardData com sucesso — clipboard assume ownership da memória
                hGlobal = IntPtr.Zero;
                return true;
            }
            return false;
        }
        finally
        {
            // CloseClipboard sempre chamado — libera para outros processos
            CloseClipboard();
            // Libera memória só se SetClipboardData falhou (não assumiu ownership)
            if (hGlobal != IntPtr.Zero)
                GlobalFree(hGlobal);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }
}
