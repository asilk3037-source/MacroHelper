using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace MacroHelper.Services;

public record AtualizacaoInfo(string Versao, string UrlDownload, bool PodeInstalar);

/// <summary>
/// Verifica o GitHub Releases e aplica atualizações via script PowerShell
/// que substitui o .exe em disco enquanto o app está fechando.
/// </summary>
public class AtualizadorService
{
    private const string ApiUrl = "https://api.github.com/repos/asilk3037-source/MacroHelper/releases/latest";

    public event EventHandler<AtualizacaoInfo>? AtualizacaoDisponivel;

    public async Task VerificarAsync()
    {
        try
        {
            var versaoAtual = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 2, 0);

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("MacroHelper-Updater/1.0");
            http.Timeout = TimeSpan.FromSeconds(10);

            var json = await http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var versaoStr = tagName.TrimStart('v');

            if (!Version.TryParse(versaoStr, out var versaoNova)) return;
            if (versaoNova <= versaoAtual) return;

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var nome = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (nome?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var u)
                            ? u.GetString() : null;
                        break;
                    }
                }
            }

            if (downloadUrl == null) return;

            // Só consegue instalar se estiver rodando como .exe publicado (não via dotnet run)
            var processPath = Environment.ProcessPath ?? string.Empty;
            var podeInstalar = processPath.EndsWith("MacroHelper.exe", StringComparison.OrdinalIgnoreCase);

            AtualizacaoDisponivel?.Invoke(this, new AtualizacaoInfo(versaoStr, downloadUrl, podeInstalar));
        }
        catch { /* verificação é silenciosa — não bloqueia a inicialização */ }
    }

    /// <summary>
    /// Baixa o novo .exe e prepara um script PowerShell que, após o app fechar,
    /// copia o novo arquivo sobre o antigo e reinicia.
    /// Retorna true se preparado com sucesso — o chamador deve fechar o app em seguida.
    /// </summary>
    public async Task<bool> PrepararAtualizacaoAsync(AtualizacaoInfo info)
    {
        try
        {
            var exeAtual = Environment.ProcessPath!;
            var tempExe  = Path.Combine(Path.GetTempPath(), "MacroHelper_update.exe");
            var tempPs1  = Path.Combine(Path.GetTempPath(), "MacroHelper_updater.ps1");

            // Download com progresso
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("MacroHelper-Updater/1.0");
            var bytes = await http.GetByteArrayAsync(info.UrlDownload);
            await File.WriteAllBytesAsync(tempExe, bytes);

            // Script PowerShell: espera o processo atual sair, substitui e reinicia
            var q      = "'";  // aspas simples — usadas como delimitador de strings PS
            var target = exeAtual.Replace("'", "''");
            var source = tempExe.Replace("'", "''");
            var self   = tempPs1.Replace("'", "''");
            var script = string.Join(Environment.NewLine, new[]
            {
                "# MacroHelper auto-updater",
                "$target = " + q + target + q,
                "$source = " + q + source + q,
                "$sw = [System.Diagnostics.Stopwatch]::StartNew()",
                "while ($sw.Elapsed.TotalSeconds -lt 15) {",
                "    try { [System.IO.File]::OpenWrite($target).Dispose(); break }",
                "    catch { Start-Sleep -Milliseconds 300 }",
                "}",
                "Copy-Item -Force -Path $source -Destination $target",
                "Start-Process -FilePath $target",
                "Remove-Item -Path $source -ErrorAction SilentlyContinue",
                "Remove-Item -Path " + q + self + q + " -ErrorAction SilentlyContinue"
            });

            await File.WriteAllTextAsync(tempPs1, script, System.Text.Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName  = "powershell.exe",
                Arguments = $"-NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{tempPs1}\"",
                UseShellExecute  = false,
                CreateNoWindow   = true
            });

            return true;
        }
        catch { return false; }
    }
}
