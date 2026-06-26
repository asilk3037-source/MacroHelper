using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;
using MacroHelper.Services;
using System.Collections.ObjectModel;
using System.IO;
using Path = System.IO.Path;
using File = System.IO.File;

namespace MacroHelper.UI.ViewModels;

public partial class RelatorioViewModel : ObservableObject
{
    private readonly LogUsoService         _logSvc;
    private readonly UsuarioService        _usuarioService;
    private readonly LogAuditoriaRepository _auditoriaRepo;
    private CancellationTokenSource? _filtroDebounceCts;

    [ObservableProperty] private ObservableCollection<LogUso> _registros = new();
    [ObservableProperty] private bool     _isLoading  = false;
    [ObservableProperty] private string   _totalHoje  = "0";
    [ObservableProperty] private string   _totalGeral = "0";
    [ObservableProperty] private DateTime _dataInicio = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _dataFim    = DateTime.Today;
    [ObservableProperty] private string   _busca      = string.Empty;
    [ObservableProperty] private string?  _mensagem;

    [ObservableProperty] private ObservableCollection<RankingItem> _ranking = new();
    [ObservableProperty] private ObservableCollection<BarraUso>    _usoPorDia = new();
    [ObservableProperty] private string  _minutosEconomizados = "0";

    [ObservableProperty] private ObservableCollection<RankingUsuarioItem> _rankingUsuarios = new();
    [ObservableProperty] private ObservableCollection<AuditoriaItem>      _auditoria = new();
    public bool IsAdmin => _usuarioService.UsuarioAtual?.Perfil == "Admin";
    public bool PodeVerRelatorioEquipe => IsAdmin || _usuarioService.UsuarioAtual?.Perfil == "Auditor"
        || Permissoes.Tem(_usuarioService.UsuarioAtual, Permissoes.VerRelatorios);

    public RelatorioViewModel(LogUsoService logSvc, UsuarioService usuarioService, LogAuditoriaRepository auditoriaRepo)
    {
        _logSvc         = logSvc;
        _usuarioService = usuarioService;
        _auditoriaRepo  = auditoriaRepo;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = (await _logSvc.ObterPorPeriodoAsync(DataInicio, DataFim.AddDays(1))).ToList();
            if (!string.IsNullOrWhiteSpace(Busca))
                lista = lista.Where(l =>
                    l.MacroTitulo.Contains(Busca, StringComparison.OrdinalIgnoreCase) ||
                    (l.Aplicativo ?? "").Contains(Busca, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            Registros   = new ObservableCollection<LogUso>(lista);
            TotalGeral  = lista.Count.ToString();
            TotalHoje   = (await _logSvc.TotalHojeAsync()).ToString();

            var top = await _logSvc.ObterTopMacrosAsync(DataInicio, DataFim.AddDays(1));
            Ranking = new ObservableCollection<RankingItem>(
                top.Select(t => new RankingItem(t.Titulo, t.Atalho, t.Total)));

            var porDia = (await _logSvc.ObterUsoPorDiaAsync(DataInicio, DataFim.AddDays(1))).ToList();
            var maxDia = porDia.Count > 0 ? porDia.Max(d => d.Total) : 1;
            UsoPorDia = new ObservableCollection<BarraUso>(
                porDia.Select(d => new BarraUso(d.Dia, d.Total, maxDia == 0 ? 0 : (double)d.Total / maxDia * 120)));

            var minutos = await _logSvc.EstimarMinutosEconomizadosAsync(DataInicio, DataFim.AddDays(1));
            MinutosEconomizados = minutos >= 60
                ? $"{minutos / 60:0.#}h"
                : $"{minutos:0}min";

            if (PodeVerRelatorioEquipe)
            {
                var topUsuarios = await _logSvc.ObterTopUsuariosAsync(DataInicio, DataFim.AddDays(1));
                RankingUsuarios = new ObservableCollection<RankingUsuarioItem>(
                    topUsuarios.Select(u => new RankingUsuarioItem(u.UsuarioNome, u.Total)));

                var auditoria = await _auditoriaRepo.GetRecentesComNomeAsync(100);
                Auditoria = new ObservableCollection<AuditoriaItem>(
                    auditoria.Select(a => new AuditoriaItem(a.Data, a.Acao, a.Entidade, a.Detalhes, a.UsuarioNome)));
            }
        }
        finally { IsLoading = false; }
    }

    partial void OnBuscaChanged(string _)        { var __ = DebounceCarregarAsync(); }
    partial void OnDataInicioChanged(DateTime _) { var __ = DebounceCarregarAsync(); }
    partial void OnDataFimChanged(DateTime _)    { var __ = DebounceCarregarAsync(); }

    /// <summary>Espera o usuário parar de digitar/ajustar datas (300ms) antes de recarregar — evita uma chamada ao banco por tecla.</summary>
    private async Task DebounceCarregarAsync()
    {
        _filtroDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _filtroDebounceCts = cts;
        try { await Task.Delay(300, cts.Token); }
        catch (TaskCanceledException) { return; }
        if (cts.IsCancellationRequested) return;
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task ExportarCsvAsync()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"MacroHelper_Relatorio_{DateTime.Now:yyyyMMdd_HHmm}.csv");

            var linhas = new List<string> { "Data,Hora,Macro,Atalho,Aplicativo" };
            foreach (var r in Registros)
                linhas.Add($"{r.DataUso:dd/MM/yyyy},{r.DataUso:HH:mm:ss}," +
                           $"\"{r.MacroTitulo}\",\"{r.MacroAtalho}\"," +
                           $"\"{r.Aplicativo ?? ""}\"");

            await File.WriteAllLinesAsync(path, linhas, System.Text.Encoding.UTF8);
            Mensagem = $"Exportado para a Área de Trabalho!";
            if (System.Threading.SynchronizationContext.Current != null)
                Task.Delay(4000).ContinueWith(_ => Mensagem = null,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch (Exception ex) { Mensagem = $"Erro: {ex.Message}"; }
    }

    [RelayCommand]
    public async Task ExportarExcelAsync()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"MacroHelper_Relatorio_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

            await Task.Run(() =>
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var sheet = workbook.Worksheets.Add("Uso de Macros");
                sheet.Cell(1, 1).Value = "Data";
                sheet.Cell(1, 2).Value = "Hora";
                sheet.Cell(1, 3).Value = "Macro";
                sheet.Cell(1, 4).Value = "Atalho";
                sheet.Cell(1, 5).Value = "Aplicativo";
                sheet.Row(1).Style.Font.Bold = true;

                var linha = 2;
                foreach (var r in Registros)
                {
                    sheet.Cell(linha, 1).Value = r.DataUso.Date;
                    sheet.Cell(linha, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                    sheet.Cell(linha, 2).Value = r.DataUso.ToString("HH:mm:ss");
                    sheet.Cell(linha, 3).Value = r.MacroTitulo;
                    sheet.Cell(linha, 4).Value = r.MacroAtalho;
                    sheet.Cell(linha, 5).Value = r.Aplicativo ?? "";
                    linha++;
                }
                sheet.Columns().AdjustToContents();
                workbook.SaveAs(path);
            });

            Mensagem = "Exportado para a Área de Trabalho!";
            if (System.Threading.SynchronizationContext.Current != null)
                Task.Delay(4000).ContinueWith(_ => Mensagem = null, TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch (Exception ex) { Mensagem = $"Erro: {ex.Message}"; }
    }

    [RelayCommand]
    public async Task ExportarRtfAsync()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"MacroHelper_Relatorio_{DateTime.Now:yyyyMMdd_HHmm}.rtf");

            var sb = new System.Text.StringBuilder();
            sb.Append(@"{\rtf1\ansi\deff0 {\fonttbl{\f0 Calibri;}}\f0\fs24 ");
            sb.Append(@"\b Relat\'f3rio de Uso - SK MacroHelper\b0\par\par ");
            foreach (var r in Registros)
                sb.Append($@"{r.DataUso:dd/MM/yyyy HH:mm} - {EscaparRtf(r.MacroTitulo)} (/{EscaparRtf(r.MacroAtalho)}) - {EscaparRtf(r.Aplicativo ?? "")}\par ");
            sb.Append("}");

            await File.WriteAllTextAsync(path, sb.ToString());
            Mensagem = "Exportado em RTF (abre direto no Word) para a Área de Trabalho!";
            if (System.Threading.SynchronizationContext.Current != null)
                Task.Delay(4000).ContinueWith(_ => Mensagem = null, TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch (Exception ex) { Mensagem = $"Erro: {ex.Message}"; }
    }

    [RelayCommand]
    public async Task ExportarHtmlParaPdfAsync()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"MacroHelper_Relatorio_{DateTime.Now:yyyyMMdd_HHmm}.html");

            var sb = new System.Text.StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'><style>body{font-family:Segoe UI,Arial;padding:24px}" +
                      "table{width:100%;border-collapse:collapse}td,th{border:1px solid #ccc;padding:6px;font-size:13px;text-align:left}</style></head><body>");
            sb.Append($"<h2>Relatório de Uso — SK MacroHelper</h2><p>Período: {DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy}</p>");
            sb.Append("<table><tr><th>Data</th><th>Macro</th><th>Atalho</th><th>Aplicativo</th></tr>");
            foreach (var r in Registros)
                sb.Append($"<tr><td>{r.DataUso:dd/MM/yyyy HH:mm}</td><td>{System.Net.WebUtility.HtmlEncode(r.MacroTitulo)}</td>" +
                          $"<td>/{System.Net.WebUtility.HtmlEncode(r.MacroAtalho)}</td><td>{System.Net.WebUtility.HtmlEncode(r.Aplicativo)}</td></tr>");
            sb.Append("</table></body></html>");

            await File.WriteAllTextAsync(path, sb.ToString());
            Mensagem = "HTML gerado! Abra e use Ctrl+P → \"Salvar como PDF\" para exportar em PDF.";
            if (System.Threading.SynchronizationContext.Current != null)
                Task.Delay(5000).ContinueWith(_ => Mensagem = null, TaskScheduler.FromCurrentSynchronizationContext());

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) { Mensagem = $"Erro: {ex.Message}"; }
    }

    private static string EscaparRtf(string texto) => texto.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
}

public record RankingItem(string Titulo, string Atalho, int Total);
public record BarraUso(DateTime Dia, int Total, double AlturaBarra);
public record RankingUsuarioItem(string UsuarioNome, int Total);
public record AuditoriaItem(DateTime Data, string Acao, string Entidade, string? Detalhes, string UsuarioNome);
