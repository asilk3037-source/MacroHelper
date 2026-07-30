using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class IntimacoesDwLawService
{
    private readonly IntimacaoErroRepository _repo;

    public IntimacoesDwLawService(IntimacaoErroRepository repo) => _repo = repo;

    public async Task<List<IntimacaoErro>> ObterTodosAsync()
        => await _repo.GetAllAsync();

    /// <summary>
    /// Importa um lote de itens extraídos do email.
    /// Se o mesmo advogado+sistema já tem um registro em aberto (não Resolvido),
    /// não insere — apenas adiciona uma nota "Erro persiste em DD/MM/YYYY" ao registro existente.
    /// Só insere como novo quando não existe nenhum registro em aberto.
    /// </summary>
    public async Task<(int inseridos, int atualizados)> ImportarLoteAsync(IEnumerable<IntimacaoErro> itens)
    {
        // Precarrega todos os registros abertos para evitar N queries ao banco (uma por item)
        var todosAbertos = await _repo.GetAllAsync();
        var abertosDict  = todosAbertos
            .Where(e => e.Status != "Resolvido")
            .GroupBy(e => (e.Advogado, e.Sistema))
            .ToDictionary(g => g.Key, g => g.ToList());

        int inseridos = 0, atualizados = 0;
        var paraAtualizar = new List<IntimacaoErro>();
        var paraInserir   = new List<IntimacaoErro>();

        foreach (var item in itens)
        {
            abertosDict.TryGetValue((item.Advogado, item.Sistema), out var abertos);
            var existente = abertos?.FirstOrDefault();

            if (existente != null)
            {
                var nota = $"Erro persiste em {item.DataEmail:dd/MM/yyyy}";
                if (existente.Observacao == null || !existente.Observacao.Contains(nota))
                {
                    existente.Observacao = string.IsNullOrWhiteSpace(existente.Observacao)
                        ? nota
                        : existente.Observacao.TrimEnd() + "\n" + nota;
                    existente.AtualizadoEm = DateTime.Now;
                    paraAtualizar.Add(existente);
                }
                atualizados++;
            }
            else
            {
                paraInserir.Add(item);
                inseridos++;
            }
        }

        foreach (var e in paraAtualizar) await _repo.UpdateAsync(e);
        foreach (var e in paraInserir)   await _repo.InsertAsync(e);

        return (inseridos, atualizados);
    }

    public async Task AtualizarStatusAsync(IntimacaoErro item, string novoStatus)
    {
        item.Status       = novoStatus;
        item.AtualizadoEm = DateTime.Now;
        item.ResolvidoEm  = novoStatus == "Resolvido" ? DateTime.Now : null;
        await _repo.UpdateAsync(item);
    }

    public async Task SalvarObservacaoAsync(IntimacaoErro item)
    {
        item.AtualizadoEm = DateTime.Now;
        await _repo.UpdateAsync(item);
    }

    public async Task ExcluirAsync(IntimacaoErro item)
        => await _repo.DeleteAsync(item.Id);

    public List<IntimacaoErro> Extrair(string texto, DateTime dataEmail)
        => DwLawEmailParser.Parse(texto, dataEmail);
}
