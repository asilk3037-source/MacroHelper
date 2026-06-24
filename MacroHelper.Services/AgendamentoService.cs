using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

/// <summary>Dispara macros agendadas (dia da semana + horário), copiando o conteúdo resolvido para a área de transferência.</summary>
public class AgendamentoService : IDisposable
{
    private readonly AgendamentoRepository _repo;
    private readonly MacroService          _macroService;
    private Timer?            _timer;
    private readonly HashSet<int> _executadosHoje = new();
    private DateTime _ultimoDia = DateTime.Today;

    public event EventHandler<(string Titulo, string Mensagem)>? AgendamentoExecutado;

    public AgendamentoService(AgendamentoRepository repo, MacroService macroService)
    {
        _repo         = repo;
        _macroService = macroService;
    }

    public async Task<IEnumerable<MacroAgendamento>> ObterTodosAsync() => await _repo.GetAllAsync();

    public async Task<(bool Ok, string Msg)> SalvarAsync(MacroAgendamento a)
    {
        if (a.MacroId == 0) return (false, "Selecione uma macro.");
        if (string.IsNullOrWhiteSpace(a.DiasSemana)) return (false, "Selecione ao menos um dia da semana.");
        if (!TimeSpan.TryParse(a.Horario, out _)) return (false, "Horário inválido. Use o formato HH:mm.");

        if (a.Id == 0) await _repo.InsertAsync(a);
        else           await _repo.UpdateAsync(a);
        return (true, "Agendamento salvo!");
    }

    public async Task ExcluirAsync(int id) => await _repo.DeleteAsync(id);

    /// <summary>Inicia a verificação periódica (a cada minuto) das macros agendadas.</summary>
    public void Iniciar() => _timer ??= new Timer(async _ => await VerificarAsync(), null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(1));

    private async Task VerificarAsync()
    {
        try
        {
            if (DateTime.Today != _ultimoDia) { _executadosHoje.Clear(); _ultimoDia = DateTime.Today; }

            var agora  = DateTime.Now;
            var ativos = (await _repo.GetAtivosAsync()).ToList();
            foreach (var a in ativos)
            {
                if (_executadosHoje.Contains(a.Id)) continue;

                var dias = a.DiasSemana.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse).ToHashSet();
                if (!dias.Contains((int)agora.DayOfWeek)) continue;
                if (!TimeSpan.TryParse(a.Horario, out var horario)) continue;
                if (Math.Abs((agora.TimeOfDay - horario).TotalMinutes) > 1.5) continue;

                await ExecutarAsync(a);
                _executadosHoje.Add(a.Id);
            }
        }
        catch { /* verificação periódica — nunca deve derrubar o app */ }
    }

    private async Task ExecutarAsync(MacroAgendamento a)
    {
        var macro = await _macroService.ObterPorIdAsync(a.MacroId);
        if (macro == null) return;

        var conteudo = await _macroService.ResolverMacrosAninhadasAsync(macro.Conteudo);
        conteudo = _macroService.ResolverCondicionais(conteudo, macro);

        CopiarParaClipboard(conteudo);
        await _repo.MarcarExecutadoAsync(a.Id, DateTime.Now);

        AgendamentoExecutado?.Invoke(this,
            ("Macro agendada", $"\"{macro.Titulo}\" foi copiada para a área de transferência."));
    }

    private static void CopiarParaClipboard(string texto)
    {
#if WINDOWS
        try { System.Windows.Clipboard.SetText(texto); } catch { }
#endif
    }

    public void Dispose() => _timer?.Dispose();
}
