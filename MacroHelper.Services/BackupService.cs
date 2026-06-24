using MacroHelper.Data.Context;
using System.IO;

namespace MacroHelper.Services;

public class BackupService : IDisposable
{
    private const int MAX_BACKUPS = 14;
    private Timer? _timer;
    private readonly WebhookService?     _webhookService;
    private readonly NotificacaoService? _notificacaoService;

    public bool   Ativo       { get; set; }
    public string PastaBackup { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MacroHelper", "Backups");

    public BackupService(WebhookService? webhookService = null, NotificacaoService? notificacaoService = null)
    {
        _webhookService = webhookService;
        _notificacaoService = notificacaoService;
    }

    public void Iniciar(bool ativo, string? pastaBackup)
    {
        Ativo = ativo;
        if (!string.IsNullOrWhiteSpace(pastaBackup)) PastaBackup = pastaBackup;
        if (!Ativo) return;

        // Primeiro backup logo na inicialização, depois a cada 24h
        _ = FazerBackupAgoraAsync();
        _timer = new Timer(_ => _ = FazerBackupAgoraAsync(), null,
            TimeSpan.FromHours(24), TimeSpan.FromHours(24));
    }

    public async Task<(bool Sucesso, string Mensagem)> FazerBackupAgoraAsync()
    {
        try
        {
            var origem = DatabaseConfig.ObterCaminhoAtivo();
            if (!File.Exists(origem)) return (false, "Banco de dados não encontrado.");

            Directory.CreateDirectory(PastaBackup);
            var destino = Path.Combine(PastaBackup, $"macros_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            await Task.Run(() => File.Copy(origem, destino, overwrite: true));
            LimparBackupsAntigos();

            if (_webhookService != null)
                await _webhookService.DispararAsync(EventosWebhook.BackupConcluido, new { destino, data = DateTime.Now });
            if (_notificacaoService != null)
                await _notificacaoService.RegistrarAsync("Backup concluído", $"Backup salvo em {Path.GetFileName(destino)}.", "Sucesso");

            return (true, $"Backup salvo em {destino}");
        }
        catch (Exception ex)
        {
            if (_notificacaoService != null)
                await _notificacaoService.RegistrarAsync("Falha no backup", ex.Message, "Erro");
            return (false, $"Erro ao fazer backup: {ex.Message}");
        }
    }

    private void LimparBackupsAntigos()
    {
        try
        {
            var arquivos = Directory.GetFiles(PastaBackup, "macros_*.db")
                .OrderByDescending(File.GetCreationTimeUtc)
                .Skip(MAX_BACKUPS);
            foreach (var f in arquivos) File.Delete(f);
        }
        catch { }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
