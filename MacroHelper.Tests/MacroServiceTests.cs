using MacroHelper.Core.Entities;
using MacroHelper.Data.Context;
using MacroHelper.Data.Repositories;
using MacroHelper.Services;

namespace MacroHelper.Tests;

public class MacroServiceFixture : IDisposable
{
    public SupabaseContext SupabaseCtx { get; }
    public MacroRepository MacroRepo { get; }
    public UsuarioRepository UsuarioRepo { get; }
    public UsuarioService UsuarioSvc { get; }
    public MacroService MacroSvc { get; }

    public MacroServiceFixture()
    {
        SupabaseCtx = new SupabaseContext();
        SupabaseCtx.InitializeAsync().GetAwaiter().GetResult();

        MacroRepo   = new MacroRepository(SupabaseCtx);
        UsuarioRepo = new UsuarioRepository(SupabaseCtx);
        UsuarioSvc  = new UsuarioService(UsuarioRepo);
        MacroSvc    = new MacroService(MacroRepo, UsuarioSvc);

        // Loga com a conta de teste dedicada no Supabase Auth (ver MACROHELPER_TEST_* no ambiente)
        // para que macros salvas nos testes saiam como "Aprovada" (precisa de perfil Admin).
        var email = Environment.GetEnvironmentVariable("MACROHELPER_TEST_EMAIL")
            ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_EMAIL para rodar os testes.");
        var senha = Environment.GetEnvironmentVariable("MACROHELPER_TEST_PASSWORD")
            ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_PASSWORD para rodar os testes.");
        UsuarioSvc.LoginAsync(email, senha).GetAwaiter().GetResult();
    }

    public void Dispose() { }
}

public class MacroServiceTests : IClassFixture<MacroServiceFixture>
{
    private readonly MacroServiceFixture _f;
    public MacroServiceTests(MacroServiceFixture f) => _f = f;

    [Fact]
    public async Task SalvarAsync_ComoAdmin_PublicaComoAprovada()
    {
        var (ok, _, macro) = await _f.MacroSvc.SalvarAsync(new Macro
        {
            Atalho = "teste-aprovada", Titulo = "Teste", Conteudo = "Conteúdo de teste"
        });

        Assert.True(ok);
        Assert.Equal("Aprovada", macro!.Status);
    }

    [Fact]
    public async Task SalvarAsync_SemUsuarioLogado_EntraEmAprovacao()
    {
        _f.UsuarioSvc.Logout();
        try
        {
            var (ok, _, macro) = await _f.MacroSvc.SalvarAsync(new Macro
            {
                Atalho = "teste-pendente", Titulo = "Teste", Conteudo = "Conteúdo de teste"
            });

            Assert.True(ok);
            Assert.Equal("Pendente", macro!.Status);
        }
        finally
        {
            // Restaura o login para não afetar os demais testes que compartilham a fixture
            var email = Environment.GetEnvironmentVariable("MACROHELPER_TEST_EMAIL")
                ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_EMAIL para rodar os testes.");
            var senha = Environment.GetEnvironmentVariable("MACROHELPER_TEST_PASSWORD")
                ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_PASSWORD para rodar os testes.");
            await _f.UsuarioSvc.LoginAsync(email, senha);
        }
    }

    [Fact]
    public async Task ResolverMacrosAninhadasAsync_ExpandeReferenciaMacro()
    {
        var atalho = $"assinatura-{Guid.NewGuid():N}";
        await _f.MacroSvc.SalvarAsync(new Macro { Atalho = atalho, Titulo = "Assinatura", Conteudo = "Equipe de Suporte" });

        var resultado = await _f.MacroSvc.ResolverMacrosAninhadasAsync($"Atenciosamente,\n{{macro:{atalho}}}");

        Assert.Contains("Equipe de Suporte", resultado);
        Assert.DoesNotContain("{macro:", resultado);
    }

    [Fact]
    public async Task ResolverMacrosAninhadasAsync_ReferenciaInexistente_RemoveToken()
    {
        var resultado = await _f.MacroSvc.ResolverMacrosAninhadasAsync("{macro:nao-existe} fim");
        Assert.Equal(" fim", resultado);
    }

    [Fact]
    public void ResolverCondicionais_MantemBlocoQuandoCategoriaCorresponde()
    {
        var macro = new Macro { Categoria = "Urgente", Conteudo = "" };
        var resultado = _f.MacroSvc.ResolverCondicionais(
            "Início {se categoria=Urgente}PRIORITÁRIO{fim} Fim", macro);

        Assert.Contains("PRIORITÁRIO", resultado);
    }

    [Fact]
    public void ResolverCondicionais_RemoveBlocoQuandoCategoriaNaoCorresponde()
    {
        var macro = new Macro { Categoria = "Normal", Conteudo = "" };
        var resultado = _f.MacroSvc.ResolverCondicionais(
            "Início {se categoria=Urgente}PRIORITÁRIO{fim} Fim", macro);

        Assert.DoesNotContain("PRIORITÁRIO", resultado);
        Assert.Contains("Início", resultado);
        Assert.Contains("Fim", resultado);
    }

    [Fact]
    public async Task DetectarPossivelDuplicataAsync_EncontraConteudoMuitoParecido()
    {
        await _f.MacroSvc.SalvarAsync(new Macro
        {
            Atalho = "original-dup", Titulo = "Original",
            Conteudo = "Prezado cliente, seu chamado foi recebido e será analisado em breve pela equipe."
        });

        var candidata = new Macro
        {
            Atalho = "parecida-dup", Titulo = "Parecida",
            Conteudo = "Prezado cliente, seu chamado foi recebido e será analisado em breve pela nossa equipe."
        };

        var duplicata = await _f.MacroSvc.DetectarPossivelDuplicataAsync(candidata);
        Assert.NotNull(duplicata);
        Assert.Equal("original-dup", duplicata!.Atalho);
    }

    [Fact]
    public async Task DetectarPossivelDuplicataAsync_RetornaNuloParaConteudoDistinto()
    {
        var candidata = new Macro { Atalho = "unico-xyz", Titulo = "Único", Conteudo = "Texto completamente diferente sobre outro assunto qualquer." };
        var duplicata = await _f.MacroSvc.DetectarPossivelDuplicataAsync(candidata);
        Assert.Null(duplicata);
    }

    [Fact]
    public async Task ExportarEImportarJson_RoundTripMantemMacro()
    {
        await _f.MacroSvc.SalvarAsync(new Macro { Atalho = "export-teste", Titulo = "Export", Conteudo = "Conteúdo exportável" });

        var json = await _f.MacroSvc.ExportarJsonAsync();
        Assert.Contains("export-teste", json);

        var (ok, _, importadas, ignoradas) = await _f.MacroSvc.ImportarJsonAsync(json);
        Assert.True(ok);
        // Já existiam ao exportar, então a reimportação deve ignorá-las como duplicadas
        Assert.True(ignoradas >= 1);
    }
}
