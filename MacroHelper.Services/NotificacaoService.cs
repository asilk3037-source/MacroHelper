using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class NotificacaoService
{
    private readonly NotificacaoRepository _repo;

    public event EventHandler<Notificacao>? NotificacaoRecebida;

    public NotificacaoService(NotificacaoRepository repo) => _repo = repo;

    public async Task RegistrarAsync(string titulo, string mensagem, string tipo = "Info")
    {
        var n = new Notificacao { Titulo = titulo, Mensagem = mensagem, Tipo = tipo };
        n.Id = await _repo.InsertAsync(n);
        NotificacaoRecebida?.Invoke(this, n);
    }

    public async Task<IEnumerable<Notificacao>> ObterRecentesAsync() => await _repo.GetRecentesAsync();

    public async Task<int> ContarNaoLidasAsync() => await _repo.ContarNaoLidasAsync();

    public async Task MarcarComoLidaAsync(int id) => await _repo.MarcarComoLidaAsync(id);

    public async Task MarcarTodasComoLidasAsync() => await _repo.MarcarTodasComoLidasAsync();

    public async Task LimparAsync() => await _repo.LimparAsync();
}
