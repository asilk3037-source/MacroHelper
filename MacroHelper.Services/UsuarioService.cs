using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.Repositories;
using System.Net;
using static Supabase.Gotrue.Constants;

namespace MacroHelper.Services;

public class UsuarioService
{
    private readonly UsuarioRepository _repo;
    private readonly NotificacaoService?  _notificacaoService;
    private readonly LogAuditoriaRepository? _auditoriaRepo;
    private readonly SupabaseContext?     _supabaseContext;
    public Usuario? UsuarioAtual { get; private set; }

    public UsuarioService(UsuarioRepository repo, NotificacaoService? notificacaoService = null,
        LogAuditoriaRepository? auditoriaRepo = null, SupabaseContext? supabaseContext = null)
    {
        _repo = repo;
        _notificacaoService = notificacaoService;
        _auditoriaRepo = auditoriaRepo;
        _supabaseContext = supabaseContext;
    }

    /// <summary>
    /// Login social via Google ou Microsoft (Supabase OAuth). Abre o navegador padrão do usuário
    /// e escuta o redirecionamento em um servidor HTTP local temporário (loopback) para capturar
    /// o código de autorização (fluxo PKCE), já que um app desktop não tem uma URL pública fixa.
    /// IMPORTANTE: requer que o provedor (Google/Azure) esteja habilitado no painel do Supabase
    /// (Authentication → Providers) com Client ID/Secret, e que "http://localhost" esteja na
    /// lista de Redirect URLs permitidas do projeto — configuração externa, fora deste código.
    /// </summary>
    public async Task<(bool Ok, string Msg, Usuario? Usuario)> LoginComProvedorAsync(
        string provedor, Action<string, string>? onSessaoObtida = null)
    {
        if (_supabaseContext == null) return (false, "Login social indisponível.", null);

        var provider = provedor.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) ? Provider.Azure : Provider.Google;

        HttpListener? listener = null;
        int porta;
        try
        {
            (listener, porta) = IniciarListenerEmPortaLivre();
        }
        catch (Exception ex)
        {
            return (false, $"Não foi possível abrir um servidor local para o login: {ex.Message}", null);
        }

        try
        {
            var redirectTo = $"http://localhost:{porta}/";
            var options = new Supabase.Gotrue.SignInOptions { FlowType = OAuthFlowType.PKCE, RedirectTo = redirectTo };
            var state = await _supabaseContext.Auth.SignIn(provider, options);
            if (state?.Uri == null) return (false, "Não foi possível iniciar o login social.", null);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(state.Uri.ToString()) { UseShellExecute = true });

            var contextTask = listener.GetContextAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
            var concluida = await Task.WhenAny(contextTask, timeoutTask);
            if (concluida == timeoutTask)
                return (false, "Tempo esgotado aguardando o login no navegador.", null);

            var httpContext = await contextTask;
            var code  = httpContext.Request.QueryString["code"];
            var erro  = httpContext.Request.QueryString["error_description"] ?? httpContext.Request.QueryString["error"];
            await ResponderNavegadorAsync(httpContext.Response, erro == null);

            if (!string.IsNullOrWhiteSpace(erro)) return (false, $"Login cancelado: {erro}", null);
            if (string.IsNullOrWhiteSpace(code)) return (false, "Login cancelado.", null);

            var session = await _supabaseContext.Auth.ExchangeCodeForSession(state.PKCEVerifier ?? "", code);
            if (session?.User?.Id == null || string.IsNullOrWhiteSpace(session.User.Email))
                return (false, "Não foi possível obter os dados da conta.", null);

            var authUserId = Guid.Parse(session.User.Id);
            var nomeSugerido = session.User.UserMetadata != null &&
                session.User.UserMetadata.TryGetValue("full_name", out var fn) ? fn?.ToString() ?? "" :
                session.User.UserMetadata != null && session.User.UserMetadata.TryGetValue("name", out var n) ? n?.ToString() ?? "" : "";

            var (usuario, ehNovo) = await _repo.ObterOuCriarViaOAuthAsync(authUserId, session.User.Email, nomeSugerido);
            if (usuario == null)
            {
                try { await _supabaseContext!.Auth.SignOut(); } catch { }
                return (false, "Conta sem acesso liberado. Fale com um administrador.", null);
            }

            UsuarioAtual = usuario;
            onSessaoObtida?.Invoke(session.AccessToken!, session.RefreshToken!);

            if (_auditoriaRepo != null)
                await _auditoriaRepo.RegistrarAsync(usuario.Id, ehNovo ? "CriarViaOAuth" : "LoginSocial", "Usuario", usuario.Id, provedor);
            if (ehNovo)
            {
                try { if (_notificacaoService != null)
                    await _notificacaoService.RegistrarAsync("Novo usuário", $"{usuario.Nome} ({usuario.Email}) entrou via login social ({provedor}).", "Sucesso"); }
                catch { /* notificação é secundária — não falha o login */ }
            }

            return (true, "Login realizado!", usuario);
        }
        catch (Exception ex)
        {
            return (false, $"Erro no login social: {ex.Message}", null);
        }
        finally
        {
            listener?.Stop();
            listener?.Close();
        }
    }

    private static (HttpListener Listener, int Porta) IniciarListenerEmPortaLivre()
    {
        for (var tentativa = 0; tentativa < 10; tentativa++)
        {
            var porta = Random.Shared.Next(49215, 65000);
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{porta}/");
            try { listener.Start(); return (listener, porta); }
            catch { /* porta ocupada — tenta outra */ }
        }
        throw new InvalidOperationException("Nenhuma porta local disponível.");
    }

    private static async Task ResponderNavegadorAsync(HttpListenerResponse resposta, bool sucesso)
    {
        var html = sucesso
            ? "<html><body style='font-family:sans-serif;text-align:center;padding-top:60px'><h2>Login concluído!</h2><p>Você já pode voltar ao SK MacroHelper.</p></body></html>"
            : "<html><body style='font-family:sans-serif;text-align:center;padding-top:60px'><h2>Login cancelado.</h2></body></html>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        resposta.ContentType = "text/html; charset=utf-8";
        resposta.ContentLength64 = bytes.Length;
        await resposta.OutputStream.WriteAsync(bytes);
        resposta.OutputStream.Close();
    }

    public async Task<(bool Ok, string Msg, Usuario? Usuario)> LoginAsync(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return (false, "Preencha e-mail e senha.", null);

        var u = await _repo.AutenticarAsync(email.Trim(), senha);
        if (u == null)
            return (false, "E-mail ou senha incorretos.", null);

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
        try { await _repo.InsertAsync(u, senha); }
        catch (Exception ex) { await AlertarSeRateLimitAsync(ex); throw; }

        try { if (_notificacaoService != null)
            await _notificacaoService.RegistrarAsync("Novo usuário", $"{u.Nome} ({u.Email}) entrou na equipe.", "Sucesso"); }
        catch { /* notificação é secundária — não falha a criação */ }

        return (true, "Usuário criado com sucesso!");
    }

    /// <summary>
    /// Detecta erros de rate-limit do Supabase Auth (envio de e-mail) e registra um alerta
    /// persistente e visível para os administradores na central de Notificações — para que o
    /// limite excedido não passe despercebido até alguém tentar criar um usuário de novo.
    /// Sempre retorna false (não trata a exceção) para que o chamador possa decidir o que fazer.
    /// </summary>
    private async Task AlertarSeRateLimitAsync(Exception ex)
    {
        if (!ex.Message.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) &&
            !ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            return;

        try { if (_notificacaoService != null)
            await _notificacaoService.RegistrarAsync(
                "Limite de e-mails do Supabase excedido",
                "O Supabase recusou o envio de e-mail (rate limit do Auth). Novas contas/convites podem falhar por alguns minutos. Aguarde e tente novamente.",
                "Aviso"); }
        catch { /* notificação é secundária */ }
    }

    public async Task<IEnumerable<Usuario>> ListarAsync() => await _repo.GetAllAsync();

    public async Task<Usuario?> ObterPorIdAsync(int id) => await _repo.GetByIdAsync(id);

    public async Task AtualizarPermissoesAsync(int usuarioId, IEnumerable<int> categoriaIdsPermitidas)
    {
        var lista = categoriaIdsPermitidas.ToList();
        var valor = lista.Count == 0 ? null : string.Join(",", lista);
        await _repo.AtualizarPermissoesAsync(usuarioId, valor);
        if (UsuarioAtual?.Id == usuarioId) UsuarioAtual.CategoriasPermitidas = valor;
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(UsuarioAtual?.Id, "AlterarCategoriasPermitidas", "Usuario", usuarioId, valor ?? "(todas)");
    }

    public async Task AtualizarPermissoesCustomAsync(int usuarioId, IEnumerable<string> chavesPermitidas)
    {
        var lista = chavesPermitidas.ToList();
        var valor = lista.Count == 0 ? null : string.Join(",", lista);
        await _repo.AtualizarPermissoesCustomAsync(usuarioId, valor);
        if (UsuarioAtual?.Id == usuarioId) UsuarioAtual.PermissoesCustom = valor;
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(UsuarioAtual?.Id, "AlterarPermissoes", "Usuario", usuarioId, valor ?? "(nenhuma)");
    }

    /// <summary>Criação de usuário pela tela de administração (Usuários), com senha definida pelo admin.</summary>
    public async Task<(bool Ok, string Msg)> CriarComoAdminAsync(string nome, string email, string senha, string perfil,
        Action<string, string>? onSessaoRestaurada = null)
    {
        if (string.IsNullOrWhiteSpace(nome))  return (false, "Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email)) return (false, "E-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6) return (false, "Senha deve ter pelo menos 6 caracteres.");
        if (await _repo.EmailExisteAsync(email)) return (false, "Este e-mail já está cadastrado.");

        var u = new Usuario { Nome = nome.Trim(), Email = email.Trim(), Perfil = perfil, Ativo = true };
        try { await _repo.InsertComoAdminAsync(u, senha, onSessaoRestaurada); }
        catch (Exception ex) { await AlertarSeRateLimitAsync(ex); throw; }

        try { if (_notificacaoService != null)
            await _notificacaoService.RegistrarAsync("Novo usuário", $"{u.Nome} ({u.Email}) entrou na equipe.", "Sucesso"); }
        catch { /* notificação é secundária — não falha a criação */ }
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(UsuarioAtual?.Id, "Criar", "Usuario", u.Id, $"{u.Nome} ({u.Email}) — perfil {u.Perfil}");

        return (true, "Usuário criado com sucesso!");
    }

    /// <summary>Edição completa (nome, e-mail, perfil, ativo) pela tela de administração.</summary>
    public async Task<(bool Ok, string Msg)> AtualizarComoAdminAsync(int usuarioId, string nome, string email, string perfil, bool ativo)
    {
        if (string.IsNullOrWhiteSpace(nome))  return (false, "Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email)) return (false, "E-mail é obrigatório.");

        var usuario = await _repo.GetByIdAsync(usuarioId);
        if (usuario == null) return (false, "Usuário não encontrado.");

        if (usuario.Perfil == "Admin" && (perfil != "Admin" || !ativo))
        {
            var todos = await _repo.GetAllAsync();
            if (todos.Count(u => u.Perfil == "Admin" && u.Ativo) <= 1)
                return (false, "Não é possível remover o último administrador ativo.");
        }

        usuario.Nome = nome.Trim(); usuario.Email = email.Trim(); usuario.Perfil = perfil; usuario.Ativo = ativo;
        await _repo.UpdateAsync(usuario);
        if (UsuarioAtual?.Id == usuarioId) { UsuarioAtual.Nome = usuario.Nome; UsuarioAtual.Email = usuario.Email; UsuarioAtual.Perfil = perfil; UsuarioAtual.Ativo = ativo; }
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(UsuarioAtual?.Id, "Editar", "Usuario", usuarioId, $"{usuario.Nome} ({usuario.Email}) — perfil {perfil}, ativo {ativo}");
        return (true, "Usuário atualizado!");
    }

    /// <summary>Remove o perfil do usuário (a conta no Supabase Auth fica órfã e não consegue mais logar).</summary>
    public async Task<(bool Ok, string Msg)> ExcluirAsync(int usuarioId)
    {
        if (UsuarioAtual?.Id == usuarioId) return (false, "Você não pode excluir o próprio usuário.");

        var usuario = await _repo.GetByIdAsync(usuarioId);
        if (usuario == null) return (false, "Usuário não encontrado.");

        if (usuario.Perfil == "Admin")
        {
            var todos = await _repo.GetAllAsync();
            if (todos.Count(u => u.Perfil == "Admin" && u.Ativo) <= 1)
                return (false, "Não é possível excluir o último administrador ativo.");
        }

        await _repo.DeleteAsync(usuarioId);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(UsuarioAtual?.Id, "Excluir", "Usuario", usuarioId, $"{usuario.Nome} ({usuario.Email})");
        return (true, "Usuário excluído.");
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
