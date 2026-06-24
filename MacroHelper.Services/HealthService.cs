using MacroHelper.Data.Context;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MacroHelper.Services;

public record InfoSaude(
    long    TamanhoBancoBytes,
    DateTime? UltimoBackup,
    int     TotalMacros,
    int     TotalUsuarios,
    long    MemoriaProcessoBytes,
    string  VersaoApp,
    bool    BancoIntegro,
    double  CpuUsoPercent,
    long    MemoriaSistemaTotalBytes,
    long    MemoriaSistemaUsadaBytes,
    long    DiscoTotalBytes,
    long    DiscoLivreBytes);

/// <summary>Reúne indicadores simples de saúde do app para o painel de Configurações.</summary>
public class HealthService
{
    private readonly MacroService    _macroService;
    private readonly UsuarioService  _usuarioService;
    private readonly BackupService   _backupService;
    private readonly DatabaseContext _dbContext;

    public HealthService(MacroService macroService, UsuarioService usuarioService,
        BackupService backupService, DatabaseContext dbContext)
    {
        _macroService   = macroService;
        _usuarioService = usuarioService;
        _backupService  = backupService;
        _dbContext      = dbContext;
    }

    public async Task<InfoSaude> ObterAsync()
    {
        var caminhoDb = DatabaseConfig.ObterCaminhoAtivo();
        var tamanhoBanco = File.Exists(caminhoDb) ? new FileInfo(caminhoDb).Length : 0;

        DateTime? ultimoBackup = null;
        try
        {
            if (Directory.Exists(_backupService.PastaBackup))
            {
                var ultimo = Directory.GetFiles(_backupService.PastaBackup, "macros_*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .FirstOrDefault();
                ultimoBackup = ultimo?.CreationTime;
            }
        }
        catch { }

        var totalMacros   = (await _macroService.ObterTodosAsync()).Count();
        var totalUsuarios = (await _usuarioService.ListarAsync()).Count();
        var memoria       = Process.GetCurrentProcess().WorkingSet64;
        var versao        = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "—";
        var bancoIntegro  = await _dbContext.VerificarIntegridadeAsync();

        var cpuUso = await ObterUsoCpuAsync();
        var (memSistemaTotal, memSistemaUsada) = ObterUsoMemoriaSistema();
        var (discoTotal, discoLivre) = ObterUsoDisco(caminhoDb);

        return new InfoSaude(tamanhoBanco, ultimoBackup, totalMacros, totalUsuarios, memoria, versao, bancoIntegro,
            cpuUso, memSistemaTotal, memSistemaUsada, discoTotal, discoLivre);
    }

    // ── Métricas da máquina local (do usuário logado nesta sessão) ──────────

    private static async Task<double> ObterUsoCpuAsync()
    {
#if !WINDOWS
        await Task.CompletedTask;
        return 0;
#else
        try
        {
            if (!GetSystemTimes(out var idle1, out var kernel1, out var user1)) return 0;
            await Task.Delay(300);
            if (!GetSystemTimes(out var idle2, out var kernel2, out var user2)) return 0;

            var idleDelta  = ParaLong(idle2)   - ParaLong(idle1);
            var kernelDelta = ParaLong(kernel2) - ParaLong(kernel1);
            var userDelta  = ParaLong(user2)   - ParaLong(user1);
            var totalDelta = kernelDelta + userDelta;
            if (totalDelta <= 0) return 0;

            var uso = 1.0 - (double)idleDelta / totalDelta;
            return Math.Clamp(uso * 100.0, 0, 100);
        }
        catch { return 0; }

        static long ParaLong(FILETIME ft) => ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
#endif
    }

    private static (long total, long usada) ObterUsoMemoriaSistema()
    {
#if !WINDOWS
        return (0, 0);
#else
        try
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (!GlobalMemoryStatusEx(ref status)) return (0, 0);
            var total = (long)status.ullTotalPhys;
            var usada = total - (long)status.ullAvailPhys;
            return (total, usada);
        }
        catch { return (0, 0); }
#endif
    }

    private static (long total, long livre) ObterUsoDisco(string caminhoReferencia)
    {
        try
        {
            var raiz  = Path.GetPathRoot(Path.GetFullPath(caminhoReferencia)) ?? Path.GetPathRoot(Environment.SystemDirectory)!;
            var drive = new DriveInfo(raiz);
            return (drive.TotalSize, drive.AvailableFreeSpace);
        }
        catch { return (0, 0); }
    }

#if WINDOWS
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
#endif
}
