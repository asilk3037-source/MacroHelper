using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MacroHelper.UI;

public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY   = 0x0312;
    private const int MOD_CONTROL = 0x0002;
    private const int VK_SPACE    = 0x20;
    private const int ID_BUSCADOR = 1;

    private HwndSource? _source;
    private IntPtr      _hwnd;
    private bool        _registrado;

    public event Action? BuscadorRapidoSolicitado;

    public void Iniciar(Window janela, int modificadores = MOD_CONTROL, int vk = VK_SPACE)
    {
        var helper = new WindowInteropHelper(janela);
        _hwnd   = helper.EnsureHandle();
        _source = HwndSource.FromHwnd(_hwnd);
        _source.AddHook(WndProc);

        _registrado = RegisterHotKey(_hwnd, ID_BUSCADOR, modificadores, vk);
    }

    /// <summary>Troca a combinação do atalho de busca rápida em tempo real. Retorna false se a combinação já estiver em uso por outro app.</summary>
    public bool Reconfigurar(int modificadores, int vk)
    {
        if (_hwnd == IntPtr.Zero) return false;
        if (_registrado) UnregisterHotKey(_hwnd, ID_BUSCADOR);
        _registrado = RegisterHotKey(_hwnd, ID_BUSCADOR, modificadores, vk);
        return _registrado;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == ID_BUSCADOR)
        {
            BuscadorRapidoSolicitado?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_registrado) UnregisterHotKey(_hwnd, ID_BUSCADOR);
        _source?.RemoveHook(WndProc);
        GC.SuppressFinalize(this);
    }

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
