using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MacroHelper.Services;

public class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN     = 0x0100;
    private const int WM_SYSKEYDOWN  = 0x0104;

    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethod.LowLevelKeyboardProc _proc;
    private string _buffer = string.Empty;

    // Buffer de texto livre (independente do gatilho) usado para detectar frases repetidas
    private string _textoLivre = string.Empty;
    private readonly Dictionary<string, int> _frasesVistas = new();
    private const int FRASE_MIN_LEN = 25;
    private const int FRASE_MAX_LEN = 400;
    private const int FRASE_LIMIAR  = 3;

    /// <summary>Caractere que inicia a detecção de gatilho. Padrão: '/'</summary>
    public char Prefixo { get; set; } = '/';

    /// <summary>Tecla final de Ctrl+Alt+&lt;tecla&gt; para repetir a última macro. Padrão: '0' (0x30).</summary>
    public int VkRepetirUltima { get; set; } = 0x30;

    /// <summary>Tecla final de Ctrl+Alt+&lt;tecla&gt; para alternar o ditado por voz. Padrão: 'V' (0x56).</summary>
    public int VkDitado { get; set; } = 0x56;

    /// <summary>Quando false, desliga a detecção de frases repetidas (sugestão proativa de IA).</summary>
    public bool DeteccaoFraseAtiva { get; set; } = true;

    public event EventHandler<string>? GatilhoDetectado;
    public event EventHandler?         GatilhoCancelado;
    public event EventHandler?         UndoSolicitado;
    public event EventHandler<string>? AtalhoTecladoPressionado;
    public event EventHandler<string>? FraseRepetidaDetectada;
    public event EventHandler?         RepetirUltimaSolicitado;
    public event EventHandler?         ProximoPlaceholderSolicitado;
    public event EventHandler?         PlaceholderAnteriorSolicitado;
    public event EventHandler?         DitadoSolicitado;

    public KeyboardHookService() => _proc = HookCallback;

    public void Iniciar()  => _hookId = SetHook(_proc);
    public void Parar()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethod.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr SetHook(NativeMethod.LowLevelKeyboardProc proc)
    {
        using var p = Process.GetCurrentProcess();
        using var m = p.MainModule!;
        return NativeMethod.SetWindowsHookEx(WH_KEYBOARD_LL, proc,
            NativeMethod.GetModuleHandle(m.ModuleName!), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            ProcessKey(vkCode);
        }
        return NativeMethod.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void ProcessKey(int vkCode)
    {
        var ctrl  = (NativeMethod.GetAsyncKeyState(0x11) & 0x8000) != 0;
        var alt   = (NativeMethod.GetAsyncKeyState(0x12) & 0x8000) != 0;
        var shift = (NativeMethod.GetAsyncKeyState(0x10) & 0x8000) != 0;

        // Ctrl+Shift+Z — desfazer última inserção
        if (ctrl && shift && vkCode == 0x5A)
        {
            UndoSolicitado?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Ctrl+Alt+1..9 — atalho de teclado dedicado por macro
        if (ctrl && alt && vkCode is >= 0x31 and <= 0x39)
        {
            var digito = ((char)vkCode).ToString();
            AtalhoTecladoPressionado?.Invoke(this, digito);
            return;
        }

        // Ctrl+Alt+<tecla> — repete a última macro inserida (tecla remapeável)
        if (ctrl && alt && vkCode == VkRepetirUltima)
        {
            RepetirUltimaSolicitado?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Ctrl+Alt+<tecla> — ativa/desativa ditado por voz (tecla remapeável)
        if (ctrl && alt && vkCode == VkDitado)
        {
            DitadoSolicitado?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Ctrl+Tab / Ctrl+Shift+Tab — navega entre placeholders {{campo}} do último texto inserido
        if (ctrl && vkCode == 0x09)
        {
            if (shift) PlaceholderAnteriorSolicitado?.Invoke(this, EventArgs.Empty);
            else       ProximoPlaceholderSolicitado?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Escape — cancela
        if (vkCode == 0x1B)
        {
            _buffer = string.Empty;
            _textoLivre = string.Empty;
            GatilhoCancelado?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Backspace
        if (vkCode == 0x08)
        {
            if (_buffer.Length > 0)
                _buffer = _buffer[..^1];
            if (_textoLivre.Length > 0)
                _textoLivre = _textoLivre[..^1];

            if (_buffer.StartsWith(Prefixo) && _buffer.Length >= 1)
                GatilhoDetectado?.Invoke(this, _buffer);
            else if (_buffer.Length == 0)
                GatilhoCancelado?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Tab — cancela gatilho, mas não conta como fim de frase
        if (vkCode == 0x09)
        {
            _buffer = string.Empty;
            GatilhoCancelado?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Enter / Space — cancela gatilho; Enter também fecha a "frase" para detecção de repetição
        if (vkCode is 0x0D or 0x20)
        {
            _buffer = string.Empty;
            GatilhoCancelado?.Invoke(this, EventArgs.Empty);
            if (vkCode == 0x0D) FecharFraseLivre();
            return;
        }

        // Ignora teclas modificadoras e de controle
        if (IsModifierOrControl(vkCode)) return;

        var c = VkToChar(vkCode);
        if (c == '\0') return;

        _buffer += c;
        AcumularTextoLivre(c);

        if (_buffer.Length > 80)
        {
            _buffer = string.Empty;
            GatilhoCancelado?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (_buffer.StartsWith(Prefixo) && _buffer.Length >= 2)
            GatilhoDetectado?.Invoke(this, _buffer);
        else if (!_buffer.StartsWith(Prefixo))
            _buffer = string.Empty;
    }

    private void AcumularTextoLivre(char c)
    {
        if (!DeteccaoFraseAtiva) return;
        _textoLivre += c;
        if (_textoLivre.Length > FRASE_MAX_LEN)
            _textoLivre = _textoLivre[^FRASE_MAX_LEN..];
    }

    private void FecharFraseLivre()
    {
        if (!DeteccaoFraseAtiva) return;
        var frase = _textoLivre.Trim();
        _textoLivre = string.Empty;
        if (frase.Length < FRASE_MIN_LEN) return;

        _frasesVistas.TryGetValue(frase, out var qtd);
        qtd++;
        _frasesVistas[frase] = qtd;

        if (qtd == FRASE_LIMIAR)
        {
            FraseRepetidaDetectada?.Invoke(this, frase);
            _frasesVistas.Remove(frase);
        }

        // Evita crescimento ilimitado do dicionário em sessões longas
        if (_frasesVistas.Count > 200) _frasesVistas.Clear();
    }

    private static bool IsModifierOrControl(int vk) =>
        vk is 0x10 or 0x11 or 0x12   // Shift, Ctrl, Alt
           or 0x5B or 0x5C            // Win keys
           or 0x14 or 0x90 or 0x91   // CapsLock, NumLock, ScrollLock
           or >= 0x21 and <= 0x2E     // Page Up/Down, End, Home, arrows, Insert, Delete
           or >= 0x70 and <= 0x87;    // F1-F24

    /// <summary>
    /// Converte virtual key para char usando o layout de teclado ATUAL do sistema
    /// (suporta ABNT2, PT-BR, US, etc.)
    /// </summary>
    private static char VkToChar(int vkCode)
    {
        // GetAsyncKeyState lê o estado físico real (hardware), independente do thread.
        // GetKeyboardState no thread do hook NÃO reflete Win key pressionada — por isso usamos GetAsyncKeyState aqui.
        if ((NativeMethod.GetAsyncKeyState(0x11) & 0x8000) != 0 || // Ctrl
            (NativeMethod.GetAsyncKeyState(0x12) & 0x8000) != 0 || // Alt
            (NativeMethod.GetAsyncKeyState(0x5B) & 0x8000) != 0 || // Win esq
            (NativeMethod.GetAsyncKeyState(0x5C) & 0x8000) != 0)   // Win dir
            return '\0';

        var state = new byte[256];
        NativeMethod.GetKeyboardState(state);

        var hwndFocus = NativeMethod.GetForegroundWindow();
        var threadId  = NativeMethod.GetWindowThreadProcessId(hwndFocus, out _);
        var hkl       = NativeMethod.GetKeyboardLayout(threadId);

        var scanCode = NativeMethod.MapVirtualKeyEx((uint)vkCode, 0, hkl);

        var buf = new char[4];
        // wFlags = 4: modo peek — lê o char sem modificar o estado interno do teclado.
        // Sem isso, ToUnicodeEx consome dead keys e interfere com atalhos do sistema (Win+V etc.)
        var result = NativeMethod.ToUnicodeEx(
            (uint)vkCode, scanCode, state, buf, buf.Length, 4, hkl);

        if (result == 1) return buf[0];
        if (result == 2) return buf[0];
        return '\0';
    }

    public void Dispose() { Parar(); GC.SuppressFinalize(this); }
}

internal static class NativeMethod
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")] public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc fn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] public static extern IntPtr GetModuleHandle(string name);
    [DllImport("user32.dll")] public static extern bool GetKeyboardState(byte[] lpKeyState);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] public static extern IntPtr GetKeyboardLayout(uint idThread);
    [DllImport("user32.dll")] public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
    [DllImport("user32.dll")] public static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, char[] pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);
}
