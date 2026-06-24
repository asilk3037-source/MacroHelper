using MacroHelper.Core.Entities;
using MacroHelper.Data.Repositories;

namespace MacroHelper.Services;

public class UsuarioService
{
    private readonly UsuarioRepository _repo;
    private readonly WebhookService?      _webhookService;
    private readonly NotificacaoService?  _notificacaoService;
    public Usuario? UsuarioAtual { get; private set; }

    public UsuarioService(UsuarioRepository repo, WebhookService? webhookService = null, NotificacaoService? notificacaoService = null)
    {
        _repo = repo;
        _webhookService = webhookService;
        _notificacaoService = notificacaoService;
    }

    public async Task<(bool Ok, string Msg, Usuario? Usuario)> LoginAsync(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return (false, "Preencha e-mail e senha.", null);

        var u = await _repo.AutenticarAsync(email.Trim(), senha);
        if (u == null) return (false, "E-mail ou senha incorretos.", null);

        UsuarioAtual = u;
        return (true, "Login realizado!", u);
    }

    public void Logout() => UsuarioAtual = null;

    /// <summary>Define o usuário atual diretamente — usado pela API para refletir o usuário do token JWT da requisição.</summary>
    public void DefinirUsuarioAtual(Usuario? usuario) => UsuarioAtual = usuario;

    public async Task<(bool Ok, string Msg)> CriarAsync(string nome, string email, string senha, string confirmar, string perfil = "Usuario")
    {
        if (string.IsNullOrWhiteSpace(nome))   return (false, "Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))  return (false, "E-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(senha))  return (false, "Senha é obrigatória.");
        if (senha != confirmar)                return (false, "As senhas não coincidem.");
        if (senha.Length < 6)                  return (false, "Senha deve ter pelo menos 6 caracteres.");
        if (await _repo.EmailExisteAsync(email)) return (false, "Este e-mail já está cadastrado.");

        var u = new Usuario { Nome = nome.Trim(), Email = email.Trim(), Perfil = perfil, Ativo = true };
        await _repo.InsertAsync(u, senha);

        if (_webhookService != null)
            await _webhookService.DispararAsync(EventosWebhook.UsuarioCriado, new { u.Nome, u.Email, u.Perfil });
        if (_notificacaoService != null)
            await _notificacaoService.RegistrarAsync("Novo usuário", $"{u.Nome} ({u.Email}) entrou na equipe.", "Sucesso");

        return (true, "Usuário criado com sucesso!");
    }

    public async Task<IEnumerable<Usuario>> ListarAsync() => await _repo.GetAllAsync();

    public async Task AtualizarPermissoesAsync(int usuarioId, IEnumerable<int> categoriaIdsPermitidas)
    {
        var lista = categoriaIdsPermitidas.ToList();
        var valor = lista.Count == 0 ? null : string.Join(",", lista);
        await _repo.AtualizarPermissoesAsync(usuarioId, valor);
        if (UsuarioAtual?.Id == usuarioId) UsuarioAtual.CategoriasPermitidas = valor;
    }

    public async Task AtualizarPermissoesCustomAsync(int usuarioId, IEnumerable<string> chavesPermitidas)
    {
        var lista = chavesPermitidas.ToList();
        var valor = lista.Count == 0 ? null : string.Join(",", lista);
        await _repo.AtualizarPermissoesCustomAsync(usuarioId, valor);
        if (UsuarioAtual?.Id == usuarioId) UsuarioAtual.PermissoesCustom = valor;
    }

    public async Task<(bool Ok, string Msg)> AtualizarPerfilAsync(int usuarioId, string nome, string email)
    {
        if (string.IsNullOrWhiteSpace(nome))  return (false, "Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email)) return (false, "E-mail é obrigatório.");

        var usuario = await _repo.GetByIdAsync(usuarioId);
        if (usuario == null) return (false, "Usuário não encontrado.");

        usuario.Nome  = nome.Trim();
        usuario.Email = email.Trim();
        await _repo.UpdateAsync(usuario);
        if (UsuarioAtual?.Id == usuarioId) { UsuarioAtual.Nome = usuario.Nome; UsuarioAtual.Email = usuario.Email; }
        return (true, "Perfil atualizado!");
    }

    public async Task<(bool Ok, string Msg)> TrocarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha)
    {
        var usuario = await _repo.GetByIdAsync(usuarioId);
        if (usuario == null) return (false, "Usuário não encontrado.");

        var autenticado = await _repo.AutenticarAsync(usuario.Email, senhaAtual);
        if (autenticado == null) return (false, "Senha atual incorreta.");
        if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
            return (false, "A nova senha deve ter pelo menos 6 caracteres.");

        await _repo.AlterarSenhaAsync(usuarioId, novaSenha);
        return (true, "Senha alterada com sucesso!");
    }
}
