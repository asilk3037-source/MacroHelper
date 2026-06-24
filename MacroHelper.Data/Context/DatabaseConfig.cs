using System.Text.Json;

namespace MacroHelper.Data.Context;

public static class DatabaseConfig
{
    private static readonly string _configFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MacroHelper", "config.json");

    private static readonly string _localDefault = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MacroHelper", "macros.db");

    public static string ObterCaminhoAtivo()
    {
        try
        {
            if (!File.Exists(_configFile)) return _localDefault;
            var json  = File.ReadAllText(_configFile);
            var cfg   = JsonSerializer.Deserialize<ConfigData>(json);
            var path  = cfg?.DatabasePath;
            return string.IsNullOrWhiteSpace(path) ? _localDefault : path;
        }
        catch { return _localDefault; }
    }

    public static void SalvarCaminho(string caminho)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configFile)!);
            var cfg  = new ConfigData { DatabasePath = caminho };
            var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFile, json);
        }
        catch { }
    }

    public static bool IsRedeLocal()
    {
        var p = ObterCaminhoAtivo();
        return p.StartsWith(@"\\") || (p.Length > 2 && p[1] == ':' && p != _localDefault);
    }

    public static string CaminhoLocal => _localDefault;

    private class ConfigData
    {
        public string? DatabasePath { get; set; }
        public string? ApiKey       { get; set; }
    }
}
