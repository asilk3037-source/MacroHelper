using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MacroHelper.UI;

/// <summary>
/// Persiste o access/refresh token do Supabase Auth em disco, criptografado via DPAPI
/// (DataProtectionScope.CurrentUser — só o usuário do Windows que gravou consegue ler),
/// para permitir login automático ao reabrir o app.
/// </summary>
public static class SupabaseSessionStore
{
    private static readonly string CaminhoArquivo = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MacroHelper", "session.dat");

    private record SessaoSalva(string AccessToken, string RefreshToken);

    public static void Save(string accessToken, string refreshToken)
    {
        var json = JsonSerializer.Serialize(new SessaoSalva(accessToken, refreshToken));
        var protegido = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);

        Directory.CreateDirectory(Path.GetDirectoryName(CaminhoArquivo)!);
        File.WriteAllBytes(CaminhoArquivo, protegido);
    }

    public static (string AccessToken, string RefreshToken)? Load()
    {
        if (!File.Exists(CaminhoArquivo)) return null;

        try
        {
            var protegido = File.ReadAllBytes(CaminhoArquivo);
            var json = Encoding.UTF8.GetString(ProtectedData.Unprotect(protegido, null, DataProtectionScope.CurrentUser));
            var sessao = JsonSerializer.Deserialize<SessaoSalva>(json);
            return sessao == null ? null : (sessao.AccessToken, sessao.RefreshToken);
        }
        catch
        {
            // Arquivo corrompido ou criptografado por outro usuário/máquina — descarta.
            Clear();
            return null;
        }
    }

    public static void Clear()
    {
        try { if (File.Exists(CaminhoArquivo)) File.Delete(CaminhoArquivo); } catch { }
    }
}
