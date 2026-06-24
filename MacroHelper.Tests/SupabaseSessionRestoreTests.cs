using MacroHelper.Data.Context;
using MacroHelper.Data.Repositories;
using MacroHelper.Services;
using MacroHelper.UI;

namespace MacroHelper.Tests;

/// <summary>
/// Simula o ciclo completo "login salva sessão" (LoginWindow.AbrirMain) → "reabrir o app
/// restaura a sessão" (App.TentarRestaurarSessaoAsync), sem depender de clicar na UI real —
/// usa duas instâncias independentes de SupabaseContext para simular dois processos do app.
/// </summary>
public class SupabaseSessionRestoreTests
{
    [Fact]
    public async Task LoginSalvaSessao_NovaInstanciaRestauraMesmoUsuario()
    {
        SupabaseSessionStore.Clear();

        var email = Environment.GetEnvironmentVariable("MACROHELPER_TEST_EMAIL")
            ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_EMAIL para rodar os testes.");
        var senha = Environment.GetEnvironmentVariable("MACROHELPER_TEST_PASSWORD")
            ?? throw new InvalidOperationException("Defina a variável de ambiente MACROHELPER_TEST_PASSWORD para rodar os testes.");

        // 1) "Processo A" — login normal, como o LoginWindow faz.
        var ctxA = new SupabaseContext();
        await ctxA.InitializeAsync();
        var usuarioRepoA = new UsuarioRepository(ctxA);
        var usuarioSvcA  = new UsuarioService(usuarioRepoA);

        var (ok, msg, usuarioLogado) = await usuarioSvcA.LoginAsync(email, senha);
        Assert.True(ok, msg);

        var sessaoA = ctxA.Auth.CurrentSession;
        Assert.NotNull(sessaoA?.AccessToken);
        Assert.NotNull(sessaoA?.RefreshToken);

        // Isso é exatamente o que LoginWindow.AbrirMain faz após LoginSucesso.
        SupabaseSessionStore.Save(sessaoA!.AccessToken!, sessaoA.RefreshToken!);

        try
        {
            // 2) "Processo B" — simula reabrir o app: instância nova, sem login manual.
            var sessaoSalva = SupabaseSessionStore.Load();
            Assert.NotNull(sessaoSalva);

            var ctxB = new SupabaseContext();
            await ctxB.InitializeAsync();
            await ctxB.Auth.SetSession(sessaoSalva!.Value.AccessToken, sessaoSalva.Value.RefreshToken);

            var authUserId = ctxB.Auth.CurrentSession?.User?.Id;
            Assert.NotNull(authUserId);

            var usuarioRepoB = new UsuarioRepository(ctxB);
            var usuarioRestaurado = await usuarioRepoB.ObterPorAuthUserIdAsync(Guid.Parse(authUserId!));

            // Isso é exatamente o que App.TentarRestaurarSessaoAsync faz no startup.
            Assert.NotNull(usuarioRestaurado);
            Assert.Equal(usuarioLogado!.Email, usuarioRestaurado!.Email);
            Assert.True(usuarioRestaurado.Ativo);
        }
        finally
        {
            SupabaseSessionStore.Clear();
        }
    }
}
