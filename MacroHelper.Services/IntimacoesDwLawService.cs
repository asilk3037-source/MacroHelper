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
    /// Marca como "Recorrente" itens em que o mesmo advogado+sistema já existe em aberto de dias anteriores.
    /// </summary>
    public async Task<int> ImportarLoteAsync(IEnumerable<IntimacaoErro> itens)
    {
        int count = 0;
        foreach (var item in itens)
        {
            // Detecta recorrência: mesmo advogado + sistema aberto em data anterior
            var anteriores = await _repo.GetAbertosByAdvSistemaAsync(item.Advogado, item.Sistema);
            var anterior = anteriores.FirstOrDefault(a => a.DataEmail.Date < item.DataEmail.Date);
            if (anterior != null)
            {
                // Se o registro anterior está "Pendente cliente", o novo herda esse status
                // (a pendência é do cliente, não um erro novo recorrente)
                item.Status = anterior.Status == "Pendente cliente"
                    ? "Pendente cliente"
                    : "Recorrente";
            }

            await _repo.InsertAsync(item);
            count++;
        }
        return count;
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
