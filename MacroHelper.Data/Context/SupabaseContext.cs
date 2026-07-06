namespace MacroHelper.Data.Context;

/// <summary>
/// Cliente Supabase (Postgres + Auth) apontando para o schema "macrohelper" do projeto Tarefas.
/// Única fonte de dados do app — não há banco local (SQLite) em paralelo.
/// </summary>
public class SupabaseContext
{
    private const string ProjectUrl = "https://pygyunefyowmbfyhbajg.supabase.co";

    // Anon key — não é segredo (modelo de segurança do Supabase é via RLS, não via sigilo da key).
    private const string AnonKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InB5Z3l1bmVmeW93bWJmeWhiYWpnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODEzNDQ4MjksImV4cCI6MjA5NjkyMDgyOX0.0M0ZqRBR50w9pR9Xd4aS9htqYBhGmLdhkA2PYPX8p74";

    public Supabase.Client Client { get; }

    public SupabaseContext()
    {
        var options = new Supabase.SupabaseOptions
        {
            Schema = "macrohelper",
            AutoConnectRealtime = false,
            AutoRefreshToken = true,
        };
        Client = new Supabase.Client(ProjectUrl, AnonKey, options);
    }

    /// <summary>Deve ser aguardado uma vez no startup, antes de qualquer repositório ser usado.</summary>
    public Task InitializeAsync() => Client.InitializeAsync();

    public Supabase.Gotrue.Interfaces.IGotrueClient<Supabase.Gotrue.User, Supabase.Gotrue.Session> Auth => Client.Auth;
}
