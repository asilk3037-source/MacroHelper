using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class LogUsoRepository
{
    private readonly SupabaseContext _ctx;
    public LogUsoRepository(SupabaseContext ctx) => _ctx = ctx;

    public async Task RegistrarAsync(LogUso log)
    {
        await _ctx.Client.From<LogUsoModel>().Insert(new LogUsoModel
        {
            MacroId = log.MacroId, MacroTitulo = log.MacroTitulo, MacroAtalho = log.MacroAtalho,
            Aplicativo = log.Aplicativo, UsuarioId = log.UsuarioId, DataUso = DateTime.Now,
            Caracteres = log.Caracteres
        });
    }

    /// <summary>Agregações (GroupBy/Count) não têm equivalente fluente no Postgrest — busca-se o
    /// período filtrado e agrega-se em C#. Volume esperado (uso de uma equipe) é pequeno.</summary>
    public async Task<IEnumerable<(string Titulo, string Atalho, int Total)>> GetTopMacrosAsync(DateTime de, DateTime ate, int limite = 10)
    {
        var periodo = await ObterPeriodoAsync(de, ate);
        return periodo
            .GroupBy(l => (l.MacroTitulo, l.MacroAtalho))
            .Select(g => (g.Key.MacroTitulo, g.Key.MacroAtalho, Total: g.Count()))
            .OrderByDescending(t => t.Total)
            .Take(limite);
    }

    public async Task<IEnumerable<(DateTime Dia, int Total)>> GetUsoPorDiaAsync(DateTime de, DateTime ate)
    {
        var periodo = await ObterPeriodoAsync(de, ate);
        return periodo
            .GroupBy(l => l.DataUso.Date)
            .Select(g => (Dia: g.Key, Total: g.Count()))
            .OrderBy(t => t.Dia);
    }

    public async Task<IEnumerable<LogUso>> GetRecentesAsync(int limite = 200)
    {
        var resp = await _ctx.Client.From<LogUsoModel>()
            .Order("data_uso", Ordering.Descending)
            .Limit(limite)
            .Get();
        return resp.Models.Select(MapToLogUso);
    }

    public async Task<IEnumerable<LogUso>> GetByPeriodoAsync(DateTime de, DateTime ate)
        => (await ObterPeriodoAsync(de, ate)).OrderByDescending(l => l.DataUso);

    public async Task<int> GetTotalHojeAsync()
    {
        var hoje = DateTime.Today;
        var resp = await _ctx.Client.From<LogUsoModel>()
            .Filter("data_uso", Operator.GreaterThanOrEqual, hoje.ToString("yyyy-MM-dd"))
            .Get();
        return resp.Models.Count;
    }

    public async Task<IEnumerable<(int? UsuarioId, string UsuarioNome, int Total)>> GetTopUsuariosAsync(DateTime de, DateTime ate, int limite = 10)
    {
        var periodo = (await ObterPeriodoAsync(de, ate)).ToList();
        var usuarioIds = periodo.Where(l => l.UsuarioId.HasValue).Select(l => l.UsuarioId!.Value).Distinct().ToList();

        var nomesPorId = new Dictionary<int, string>();
        if (usuarioIds.Count > 0)
        {
            var usuarios = await _ctx.Client.From<UsuariosModel>()
                .Filter("id", Operator.In, usuarioIds.Select(id => (object)id).ToList())
                .Get();
            nomesPorId = usuarios.Models.ToDictionary(u => u.Id, u => u.Nome);
        }

        return periodo
            .GroupBy(l => l.UsuarioId)
            .Select(g => (g.Key, g.Key.HasValue && nomesPorId.TryGetValue(g.Key.Value, out var nome) ? nome : "Desconhecido", Total: g.Count()))
            .OrderByDescending(t => t.Total)
            .Take(limite);
    }

    /// <summary>Agrega registros antigos em LogUsoResumo (somando aos totais já existentes) e remove
    /// os detalhes originais. Equivalente ao "INSERT ... ON CONFLICT DO UPDATE" do SQLite, feito
    /// aqui em duas etapas porque não é uma tarefa de caminho crítico (job de manutenção).</summary>
    public async Task<int> ArquivarAnterioresAsync(DateTime limite)
    {
        var antigos = await _ctx.Client.From<LogUsoModel>()
            .Filter("data_uso", Operator.LessThan, limite.ToString("yyyy-MM-dd HH:mm:ss"))
            .Get();
        var lista = antigos.Models;

        var grupos = lista.GroupBy(l => (AnoMes: l.DataUso.ToString("yyyy-MM"), l.MacroTitulo, l.MacroAtalho));
        foreach (var grupo in grupos)
        {
            var total = grupo.Count();
            var caracteres = grupo.Sum(l => l.Caracteres);

            var existente = await _ctx.Client.From<LogUsoResumoModel>()
                .Filter("ano_mes", Operator.Equals, grupo.Key.AnoMes)
                .Filter("macro_atalho", Operator.Equals, grupo.Key.MacroAtalho)
                .Single();

            if (existente != null)
            {
                existente.Total += total;
                existente.Caracteres += caracteres;
                await _ctx.Client.From<LogUsoResumoModel>().Update(existente);
            }
            else
            {
                await _ctx.Client.From<LogUsoResumoModel>().Insert(new LogUsoResumoModel
                {
                    AnoMes = grupo.Key.AnoMes, MacroTitulo = grupo.Key.MacroTitulo,
                    MacroAtalho = grupo.Key.MacroAtalho, Total = total, Caracteres = caracteres
                });
            }
        }

        foreach (var l in lista)
            await _ctx.Client.From<LogUsoModel>().Filter("id", Operator.Equals, l.Id.ToString()).Delete();

        return lista.Count;
    }

    public async Task<IEnumerable<(string AnoMes, int Total)>> GetResumoArquivadoAsync()
    {
        var resp = await _ctx.Client.From<LogUsoResumoModel>().Get();
        return resp.Models
            .GroupBy(r => r.AnoMes)
            .Select(g => (AnoMes: g.Key, Total: g.Sum(r => r.Total)))
            .OrderByDescending(t => t.AnoMes);
    }

    private async Task<List<LogUso>> ObterPeriodoAsync(DateTime de, DateTime ate)
    {
        var resp = await _ctx.Client.From<LogUsoModel>()
            .Filter("data_uso", Operator.GreaterThanOrEqual, de.ToString("yyyy-MM-dd HH:mm:ss"))
            .Filter("data_uso", Operator.LessThanOrEqual, ate.ToString("yyyy-MM-dd HH:mm:ss"))
            .Get();
        return resp.Models.Select(MapToLogUso).ToList();
    }

    private static LogUso MapToLogUso(LogUsoModel m) => new()
    {
        Id = m.Id, MacroId = m.MacroId, MacroTitulo = m.MacroTitulo, MacroAtalho = m.MacroAtalho,
        Aplicativo = m.Aplicativo, UsuarioId = m.UsuarioId, DataUso = m.DataUso, Caracteres = m.Caracteres
    };
}
