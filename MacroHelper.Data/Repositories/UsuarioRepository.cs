using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.SupabaseModels;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace MacroHelper.Data.Repositories;

public class UsuarioRepository
{
    private readonly SupabaseContext _ctx;
    public UsuarioRepository(SupabaseContext ctx) => _ctx = ctx;

    /// <summary>
    /// Autentica via Supabase Auth. Se o perfil em macrohelper.usuarios ainda não estiver
    /// ligado (auth_user_id IS NULL — caso de uma linha semeada como o admin), faz a ligação
    /// pelo e-mail no primeiro login bem-sucedido.
    /// </summary>
    public async Task<Usuario?> AutenticarAsync(string email, string senha)
    {
        email = email.Trim().ToLower();

        Supabase.Gotrue.Session? session;
        try { session = await _ctx.Auth.SignInWithPassword(email, senha); }
        catch { return null; }
        if (session?.User?.Id == null) return null;

        var authUserId = Guid.Parse(session.User.Id);

        var perfil = await _ctx.Client.From<UsuariosModel>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Single();

        if (perfil == null)
        {
            perfil = await _ctx.Client.From<UsuariosModel>()
                .Filter("email", Operator.Equals, email)
                .Filter("auth_user_id", Operator.Is, "null")
                .Single();

            if (perfil == null) return null;

            perfil.AuthUserId = authUserId;
            await _ctx.Client.From<UsuariosModel>().Update(perfil);
        }

        return perfil.Ativo ? MapToUsuario(perfil) : null;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        var resp = await _ctx.Client.From<UsuariosModel>().Order("nome", Ordering.Ascending).Get();
        return resp.Models.Select(MapToUsuario);
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        var m = await _ctx.Client.From<UsuariosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        return m == null ? null : MapToUsuario(m);
    }

    public async Task<bool> EmailExisteAsync(string email)
    {
        var resp = await _ctx.Client.From<UsuariosModel>()
            .Filter("email", Operator.Equals, email.Trim().ToLower())
            .Get();
        return resp.Models.Count > 0;
    }

    public async Task<int> InsertAsync(Usuario u, string senha)
    {
        u.Email = u.Email.Trim().ToLower();

        var signUp = await _ctx.Auth.SignUp(u.Email, senha);
        if (signUp?.User?.Id == null)
            throw new InvalidOperationException("Falha ao criar usuário no Supabase Auth.");

        var model = new UsuariosModel
        {
            AuthUserId  = Guid.Parse(signUp.User.Id),
            Nome        = u.Nome,
            Email       = u.Email,
            Perfil      = u.Perfil,
            Ativo       = u.Ativo,
            DataCriacao = DateTime.Now
        };
        var inserted = await _ctx.Client.From<UsuariosModel>().Insert(model);
        return inserted.Models.First().Id;
    }

    public async Task UpdateAsync(Usuario u)
    {
        var m = await _ctx.Client.From<UsuariosModel>().Filter("id", Operator.Equals, u.Id.ToString()).Single();
        if (m == null) return;
        m.Nome = u.Nome; m.Email = u.Email; m.Perfil = u.Perfil; m.Ativo = u.Ativo;
        await _ctx.Client.From<UsuariosModel>().Update(m);
    }

    public async Task DeleteAsync(int id)
    {
        await _ctx.Client.From<UsuariosModel>().Filter("id", Operator.Equals, id.ToString()).Delete();
    }

    /// <summary>
    /// Cria um usuário a partir da tela de administração (admin já está logado).
    /// SignUp troca a sessão ativa do cliente Supabase para o usuário recém-criado — por isso
    /// a sessão do admin é capturada antes e restaurada depois, evitando deslogar quem está criando.
    /// O refresh token é rotativo (uso único): <paramref name="onSessaoRestaurada"/> recebe o par
    /// (access, refresh) já renovado para a camada de UI persistir em session.dat.
    /// </summary>
    public async Task<int> InsertComoAdminAsync(Usuario u, string senha, Action<string, string>? onSessaoRestaurada = null)
    {
        u.Email = u.Email.Trim().ToLower();
        var sessaoAdmin = _ctx.Auth.CurrentSession;

        var signUp = await _ctx.Auth.SignUp(u.Email, senha);
        if (signUp?.User?.Id == null)
            throw new InvalidOperationException("Falha ao criar usuário no Supabase Auth.");

        var model = new UsuariosModel
        {
            AuthUserId  = Guid.Parse(signUp.User.Id),
            Nome        = u.Nome,
            Email       = u.Email,
            Perfil      = u.Perfil,
            Ativo       = u.Ativo,
            DataCriacao = DateTime.Now
        };
        var inserted = await _ctx.Client.From<UsuariosModel>().Insert(model);

        if (sessaoAdmin != null)
        {
            await _ctx.Auth.SetSession(sessaoAdmin.AccessToken!, sessaoAdmin.RefreshToken!);
            var renovada = _ctx.Auth.CurrentSession;
            if (renovada?.AccessToken != null && renovada.RefreshToken != null)
                onSessaoRestaurada?.Invoke(renovada.AccessToken, renovada.RefreshToken);
        }

        return inserted.Models.First().Id;
    }

    public async Task AtualizarPermissoesAsync(int id, string? categoriasPermitidas)
    {
        var m = await _ctx.Client.From<UsuariosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.CategoriasPermitidas = categoriasPermitidas;
        await _ctx.Client.From<UsuariosModel>().Update(m);
    }

    public async Task AtualizarPermissoesCustomAsync(int id, string? permissoesCustom)
    {
        var m = await _ctx.Client.From<UsuariosModel>().Filter("id", Operator.Equals, id.ToString()).Single();
        if (m == null) return;
        m.PermissoesCustom = permissoesCustom;
        await _ctx.Client.From<UsuariosModel>().Update(m);
    }

    /// <summary>
    /// Troca a senha do usuário ATUALMENTE autenticado na sessão do Supabase Auth.
    /// Não suporta "admin troca senha de outro usuário" — UsuarioService.TrocarSenhaAsync já
    /// reautentica com a senha atual antes de chamar este método, então a sessão ativa é
    /// sempre a do próprio usuário cuja senha está sendo trocada.
    /// </summary>
    public async Task AlterarSenhaAsync(int id, string novaSenha)
    {
        await _ctx.Auth.Update(new Supabase.Gotrue.UserAttributes { Password = novaSenha });
    }

    /// <summary>Usado na restauração de sessão do desktop no startup (sem senha disponível).</summary>
    public async Task<Usuario?> ObterPorAuthUserIdAsync(Guid authUserId)
    {
        var m = await _ctx.Client.From<UsuariosModel>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Single();
        return m == null ? null : MapToUsuario(m);
    }

    /// <summary>
    /// Usado após login social (Google/Microsoft): vincula o auth_user_id a um perfil existente
    /// pelo e-mail (caso já exista, ex: semeado por um admin) ou cria um novo perfil "Usuario".
    /// </summary>
    public async Task<(Usuario? Usuario, bool EhNovo)> ObterOuCriarViaOAuthAsync(Guid authUserId, string email, string nomeSugerido)
    {
        email = email.Trim().ToLower();

        var porAuthId = await _ctx.Client.From<UsuariosModel>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Single();
        if (porAuthId != null) return (porAuthId.Ativo ? MapToUsuario(porAuthId) : null, false);

        var porEmail = await _ctx.Client.From<UsuariosModel>()
            .Filter("email", Operator.Equals, email)
            .Filter("auth_user_id", Operator.Is, "null")
            .Single();
        if (porEmail != null)
        {
            porEmail.AuthUserId = authUserId;
            await _ctx.Client.From<UsuariosModel>().Update(porEmail);
            return (porEmail.Ativo ? MapToUsuario(porEmail) : null, false);
        }

        var novo = new UsuariosModel
        {
            AuthUserId  = authUserId,
            Nome        = string.IsNullOrWhiteSpace(nomeSugerido) ? email : nomeSugerido,
            Email       = email,
            Perfil      = "Usuario",
            Ativo       = true,
            DataCriacao = DateTime.Now
        };
        var inserted = await _ctx.Client.From<UsuariosModel>().Insert(novo);
        return (MapToUsuario(inserted.Models.First()), true);
    }

    private static Usuario MapToUsuario(UsuariosModel m) => new()
    {
        Id                   = m.Id,
        Nome                 = m.Nome,
        Email                = m.Email,
        SenhaHash            = string.Empty, // senha agora é gerenciada pelo Supabase Auth
        Perfil               = m.Perfil,
        Ativo                = m.Ativo,
        DataCriacao          = m.DataCriacao,
        CategoriasPermitidas = m.CategoriasPermitidas,
        GrupoId              = m.GrupoId,
        PermissoesCustom     = m.PermissoesCustom
    };
}
