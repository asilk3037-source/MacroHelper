using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.Repositories;
using MacroHelper.Services;
using MacroHelper.UI.ViewModels;
using System.IO;

namespace MacroHelper.Tests;

/// <summary>
/// Fixture com banco SQLite real (arquivo temporário) e todos os serviços usados pelas
/// 12 telas novas, para validar que cada tela efetivamente carrega dados do banco.
/// </summary>
public class NovasTelasFixture : IDisposable
{
    public UsuarioService          UsuarioSvc       { get; }
    public MacroService            MacroSvc         { get; }
    public LogUsoService           LogUsoSvc        { get; }
    public VariavelGlobalService   VariavelSvc      { get; }
    public AgendamentoService      AgendamentoSvc   { get; }
    public GrupoService            GrupoSvc         { get; }
    public NotificacaoService      NotificacaoSvc   { get; }
    public PermissaoTelaService    PermissaoTelaSvc { get; }
    public LogAuditoriaRepository  AuditoriaRepo    { get; }

    public NovasTelasFixture()
    {
        var supabaseCtx = new SupabaseContext();
        supabaseCtx.InitializeAsync().GetAwaiter().GetResult();

        var usuarioRepo  = new UsuarioRepository(supabaseCtx);
        UsuarioSvc       = new UsuarioService(usuarioRepo);

        var macroRepo    = new MacroRepository(supabaseCtx);
        var versaoRepo   = new MacroVersaoRepository(supabaseCtx);
        AuditoriaRepo    = new LogAuditoriaRepository(supabaseCtx);
        var favoritoRepo = new FavoritoUsuarioRepository(supabaseCtx);
        var varGlobalRepo = new VariavelGlobalRepository(supabaseCtx);
        VariavelSvc      = new VariavelGlobalService(varGlobalRepo);

        MacroSvc = new MacroService(macroRepo, UsuarioSvc, versaoRepo, AuditoriaRepo, favoritoRepo, VariavelSvc);

        var logUsoRepo   = new LogUsoRepository(supabaseCtx);
        LogUsoSvc        = new LogUsoService(logUsoRepo);

        var agendamentoRepo = new AgendamentoRepository(supabaseCtx);
        AgendamentoSvc      = new AgendamentoService(agendamentoRepo, MacroSvc);

        var grupoRepo = new GrupoRepository(supabaseCtx);
        GrupoSvc      = new GrupoService(grupoRepo);

        var notificacaoRepo = new NotificacaoRepository(supabaseCtx);
        NotificacaoSvc      = new NotificacaoService(notificacaoRepo);

        var permissaoTelaRepo = new PermissaoTelaRepository(supabaseCtx);
        PermissaoTelaSvc      = new PermissaoTelaService(permissaoTelaRepo, UsuarioSvc);

        var email = Environment.GetEnvironmentVariable("MACROHELPER_TEST_EMAIL")
            ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_EMAIL para rodar os testes.");
        var senha = Environment.GetEnvironmentVariable("MACROHELPER_TEST_PASSWORD")
            ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_PASSWORD para rodar os testes.");
        UsuarioSvc.LoginAsync(email, senha).GetAwaiter().GetResult();
    }

    public void Dispose() { }
}

public class NovasTelasTests : IClassFixture<NovasTelasFixture>
{
    private readonly NovasTelasFixture _f;
    public NovasTelasTests(NovasTelasFixture f) => _f = f;

    [Fact]
    public async Task Dashboard_CarregarAsync_TrazMetricasReais()
    {
        await _f.MacroSvc.SalvarAsync(new Macro { Atalho = "dash-1", Titulo = "Dash 1", Conteudo = "Conteúdo dashboard" });
        await _f.LogUsoSvc.RegistrarAsync(null, "Dash 1", "dash-1", _f.UsuarioSvc.UsuarioAtual?.Id, 50);

        var vm = new DashboardViewModel(_f.MacroSvc, _f.LogUsoSvc, _f.UsuarioSvc);
        await vm.CarregarAsync();

        Assert.True(vm.TotalMacros > 0);
        Assert.False(vm.IsLoading);
        Assert.NotEmpty(vm.AtividadeRecente);
    }

    [Fact]
    public async Task VariaveisGlobais_CarregarAsync_TrazVariavelSalva()
    {
        var nome = $"empresa_{Guid.NewGuid():N}";
        await _f.VariavelSvc.SalvarAsync(new VariavelGlobal { Nome = nome, ValorPadrao = "ACME" });

        var vm = new VariaveisGlobaisViewModel(_f.VariavelSvc);
        await vm.CarregarAsync();

        Assert.Contains(vm.Variaveis, v => v.Nome == nome);
    }

    [Fact]
    public async Task HistoricoVersoes_SelecionarMacro_TrazVersaoEDiff()
    {
        var atalho = $"hist-1-{Guid.NewGuid():N}";
        var (_, _, macro) = await _f.MacroSvc.SalvarAsync(new Macro { Atalho = atalho, Titulo = "Histórico", Conteudo = "Versão 1" });
        macro!.Conteudo = "Versão 2";
        await _f.MacroSvc.SalvarAsync(macro);

        var vm = new HistoricoVersoesViewModel(_f.MacroSvc);
        await vm.CarregarAsync();
        Assert.Contains(vm.Macros, m => m.Atalho == atalho);

        var atualizada = vm.Macros.First(m => m.Atalho == atalho);
        await vm.SelecionarMacroAsync(atualizada);

        Assert.NotEmpty(vm.Versoes);
        Assert.NotEmpty(vm.Diff);
        Assert.Contains(vm.Diff, l => l.Diferente);
    }

    [Fact]
    public async Task Agendamentos_CarregarAsync_TrazAgendamentoSalvo()
    {
        var atalho = $"agenda-1-{Guid.NewGuid():N}";
        var (_, _, macro) = await _f.MacroSvc.SalvarAsync(new Macro { Atalho = atalho, Titulo = "Agendada", Conteudo = "Conteúdo agendado" });
        var (ok, msg) = await _f.AgendamentoSvc.SalvarAsync(new MacroAgendamento { MacroId = macro!.Id, DiasSemana = "1,2,3,4,5", Horario = "09:00" });
        Assert.True(ok, msg);

        var vm = new AgendamentosViewModel(_f.AgendamentoSvc, _f.MacroSvc);
        await vm.CarregarAsync();

        Assert.NotEmpty(vm.Agendamentos);
        Assert.NotEmpty(vm.MacrosDisponiveis);
    }

    [Fact]
    public async Task Grupos_CarregarAsync_TrazGrupoEUsuarios()
    {
        var nome = $"Suporte N2 {Guid.NewGuid():N}";
        var (ok, msg) = await _f.GrupoSvc.CriarAsync(nome);
        Assert.True(ok, msg);

        var vm = new GruposViewModel(_f.GrupoSvc, _f.UsuarioSvc);
        await vm.CarregarAsync();

        Assert.Contains(vm.Grupos, g => g.Nome == nome);
        Assert.NotEmpty(vm.TodosUsuarios);
    }

    [Fact]
    public async Task Auditoria_CarregarAsync_TrazRegistroDeEdicao()
    {
        var atalho = $"audit-1-{Guid.NewGuid():N}";
        var (_, _, macro) = await _f.MacroSvc.SalvarAsync(new Macro { Atalho = atalho, Titulo = "Audit", Conteudo = "Original" });
        macro!.Conteudo = "Editado";
        await _f.MacroSvc.SalvarAsync(macro); // gera registro de auditoria "Editar"

        var vm = new AuditoriaViewModel(_f.AuditoriaRepo);
        await vm.CarregarAsync();

        Assert.NotEmpty(vm.Registros);
        Assert.Contains(vm.Registros, r => r.Acao == "Editar" && r.Entidade == "Macro");
    }

    [Fact]
    public async Task Comunidade_CarregarECurtir_FuncionaDePontaAPonta()
    {
        var atalho = $"comunidade-1-{Guid.NewGuid():N}";
        var (_, _, macro) = await _f.MacroSvc.SalvarAsync(new Macro { Atalho = atalho, Titulo = "Pública", Conteudo = "Conteúdo público" });
        await _f.MacroSvc.TogglePublicaAsync(macro!);

        var vm = new ComunidadeViewModel(_f.MacroSvc);
        await vm.CarregarAsync();
        Assert.NotEmpty(vm.MacrosPublicas);

        var publicada = vm.MacrosPublicas.First(m => m.Atalho == atalho);
        await vm.CurtirCommand.ExecuteAsync(publicada);
        await vm.CarregarAsync();

        var atualizada = vm.MacrosPublicas.First(m => m.Atalho == atalho);
        Assert.True(atualizada.CurtidaPeloUsuario);
        Assert.Equal(1, atualizada.TotalCurtidas);

        await vm.CopiarParaMinhasCommand.ExecuteAsync(publicada);
        Assert.True(vm.MensagemSucesso);
        Assert.Contains(await _f.MacroSvc.ObterTodosAsync(), m => m.Atalho == $"{atalho}-copia");
    }

    [Fact]
    public async Task Permissoes_CarregarESalvar_PersisteNoBanco()
    {
        // CriarAsync chama Auth.SignUp, que troca a sessão ativa da fixture compartilhada
        // (e-mail de confirmação pendente deixa sem sessão válida) — o finally relogar como
        // admin para não corromper a sessão dos demais testes que usam a mesma fixture,
        // mesmo se este teste falhar (ex.: rate limit de envio de e-mail do Supabase Auth).
        try
        {
            // E-mail fixo (não um novo a cada execução): Auth.SignUp tem rate limit de envio
            // de e-mail no Supabase, então só criamos de verdade na primeira execução —
            // nas próximas, CriarAsync retorna "já cadastrado" e reaproveitamos o usuário.
            var emailNovo = "equipe.teste@macrohelper.com";
            await _f.UsuarioSvc.CriarAsync("Equipe Teste", emailNovo, "senha123", "senha123");

            var vm = new PermissoesViewModel(_f.UsuarioSvc, _f.PermissaoTelaSvc);
            await vm.CarregarAsync();

            Assert.NotEmpty(vm.Usuarios);
            Assert.NotNull(vm.UsuarioSelecionado);

            var criado = vm.Usuarios.First(u => u.Email == emailNovo);
            vm.SelecionarUsuarioCommand.Execute(criado);
            Assert.Equal(Permissoes.Todas.Length, vm.Opcoes.Count);

            var opcao = vm.Opcoes.First();
            opcao.Concedida = true;
            await vm.SalvarAsync();

            var usuarios = await _f.UsuarioSvc.ListarAsync();
            var salvo = usuarios.First(u => u.Email == emailNovo);
            Assert.Contains(opcao.Chave, salvo.PermissoesCustom ?? "", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            var email = Environment.GetEnvironmentVariable("MACROHELPER_TEST_EMAIL")
                ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_EMAIL para rodar os testes.");
            var senha = Environment.GetEnvironmentVariable("MACROHELPER_TEST_PASSWORD")
                ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_PASSWORD para rodar os testes.");
            await _f.UsuarioSvc.LoginAsync(email, senha);
        }
    }

    [Fact]
    public async Task Notificacoes_CarregarEMarcarComoLida_AtualizaEstado()
    {
        await _f.NotificacaoSvc.RegistrarAsync("Backup concluído", "O backup automático foi concluído com sucesso.", "Sucesso");

        var vm = new NotificacoesViewModel(_f.NotificacaoSvc);
        await vm.CarregarAsync();

        Assert.NotEmpty(vm.Notificacoes);
        var naoLida = vm.Notificacoes.First(n => n.Titulo == "Backup concluído");
        Assert.False(naoLida.Lida);

        await vm.MarcarComoLidaCommand.ExecuteAsync(naoLida);

        var lida = vm.Notificacoes.First(n => n.Titulo == "Backup concluído");
        Assert.True(lida.Lida);
    }

    [Fact]
    public async Task Perfil_DadosDoUsuarioLogado_SaoCarregadosNoConstrutor()
    {
        var vm = new PerfilViewModel(_f.UsuarioSvc);

        Assert.Equal("Administrador", vm.Nome.Length > 0 ? vm.Perfil : vm.Perfil);
        Assert.Equal(_f.UsuarioSvc.UsuarioAtual!.Email, vm.Email);
        Assert.Equal("Administrador", vm.Perfil);

        vm.NovaSenha = "abc"; vm.ConfirmarSenha = "xyz";
        await vm.TrocarSenhaCommand.ExecuteAsync(null);
        Assert.False(vm.MensagemSenhaSucesso);
        Assert.Equal("As senhas não coincidem.", vm.MensagemSenha);
    }

    [Fact]
    public void Ajuda_ListaDePerguntasFrequentesEstaPreenchida()
    {
        var vm = new AjudaViewModel();

        Assert.NotEmpty(vm.Faqs);
        Assert.All(vm.Faqs, f => Assert.False(string.IsNullOrWhiteSpace(f.Pergunta)));
        Assert.All(vm.Faqs, f => Assert.False(string.IsNullOrWhiteSpace(f.Resposta)));
        Assert.Equal("1.0.0", vm.VersaoApp);
    }
}
