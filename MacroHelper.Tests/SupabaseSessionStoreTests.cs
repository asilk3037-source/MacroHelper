using MacroHelper.UI;

namespace MacroHelper.Tests;

public class SupabaseSessionStoreTests
{
    [Fact]
    public void SaveLoadClear_RoundtripFunciona()
    {
        SupabaseSessionStore.Clear();
        Assert.Null(SupabaseSessionStore.Load());

        SupabaseSessionStore.Save("access-teste-123", "refresh-teste-456");
        var sessao = SupabaseSessionStore.Load();

        Assert.NotNull(sessao);
        Assert.Equal("access-teste-123", sessao!.Value.AccessToken);
        Assert.Equal("refresh-teste-456", sessao.Value.RefreshToken);

        SupabaseSessionStore.Clear();
        Assert.Null(SupabaseSessionStore.Load());
    }
}
