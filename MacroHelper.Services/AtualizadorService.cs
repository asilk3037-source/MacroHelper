using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace MacroHelper.Services;

public record AtualizacaoInfo(string Versao, string UrlDownload, bool PodeInstalar);

/// <summary>
/// Verifica o GitHub Releases e aplica atualizações via script PowerShell
/// que extrai o ZIP e substitui a pasta do app enquanto ele está fechando.
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
                    if (nome?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var u)
                            ? u.GetString() : null;
                        break;
                    }
                }
            }

            if (downloadUrl == null) return;

            var processPath = Environment.ProcessPath ?? string.Empty;
            var podeInstalar = processPath.EndsWith("MacroHelper.exe", StringComparison.OrdinalIgnoreCase);

            AtualizacaoDisponivel?.Invoke(this, new AtualizacaoInfo(versaoStr, downloadUrl, podeInstalar));
        }
        catch { /* verificação é silenciosa */ }
    }

    /// <summary>
    /// Baixa o ZIP, extrai em pasta temporária e prepara script PowerShell que,
    /// após o app fechar, copia os novos arquivos sobre a pasta atual e reinicia.
    /// </summary>
    public async Task<bool> PrepararAtualizacaoAsync(AtualizacaoInfo info)
    {
        try
        {
            var exeAtual    = Environment.ProcessPath!;
            var pastaApp    = Path.GetDirectoryName(exeAtual)!;
            var tempZip     = Path.Combine(Path.GetTempPath(), "MacroHelper_update.zip");
            var tempExtract = Path.Combine(Path.GetTempPath(), "MacroHelper_update_extract");
            var tempPs1     = Path.Combine(Path.GetTempPath(), "MacroHelper_updater.ps1");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("MacroHelper-Updater/1.0");
            var bytes = await http.GetByteArrayAsync(info.UrlDownload);
            await File.WriteAllBytesAsync(tempZip, bytes);

            // Extrai antecipadamente para não depender do PowerShell ter acesso ao ZIP
            if (Directory.Exists(tempExtract))
                Directory.Delete(tempExtract, recursive: true);
            ZipFile.ExtractToDirectory(tempZip, tempExtract);
            File.Delete(tempZip);

            var q       = "'";
            var srcDir  = tempExtract.Replace("'", "''");
            var dstDir  = pastaApp.Replace("'", "''");
            var exePath = exeAtual.Replace("'", "''");
            var self    = tempPs1.Replace("'", "''");

            var script = string.Join(Environment.NewLine, new[]
            {
                "# MacroHelper auto-updater",
                "$src = " + q + srcDir + q,
                "$dst = " + q + dstDir + q,
                "$exe = " + q + exePath + q,
                "$sw  = [System.Diagnostics.Stopwatch]::StartNew()",
                "while ($sw.Elapsed.TotalSeconds -lt 20) {",
                "    try { [System.IO.File]::OpenWrite($exe).Dispose(); break }",
                "    catch { Start-Sleep -Milliseconds 400 }",
                "}",
                "Get-ChildItem -Path $src | ForEach-Object {",
                "    Copy-Item -Path $_.FullName -Destination $dst -Recurse -Force",
                "}",
                "Remove-Item -Path $src -Recurse -Force -ErrorAction SilentlyContinue",
                "Start-Process -FilePath $exe",
                "Remove-Item -Path " + q + self + q + " -ErrorAction SilentlyContinue"
            });

            await File.WriteAllTextAsync(tempPs1, script, System.Text.Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = $"-NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{tempPs1}\"",
                UseShellExecute = false,
                CreateNoWindow  = true
            });

            return true;
        }
        catch { return false; }
    }
}
