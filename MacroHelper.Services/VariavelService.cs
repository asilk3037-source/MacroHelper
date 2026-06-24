using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MacroHelper.Services;

public class VariavelInfo : INotifyPropertyChanged
{
    private string? _valorPadrao;

    public string  Nome          { get; set; } = string.Empty;
    public string  Placeholder   { get; set; } = string.Empty;
    public bool    AutoPreencher { get; set; }

    public string? ValorPadrao
    {
        get => _valorPadrao;
        set { _valorPadrao = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public static class VariavelService
{
    private static readonly Regex _regex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    public static List<VariavelInfo> ExtrairVariaveis(string template, string nomeUsuario = "")
    {
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lista  = new List<VariavelInfo>();

        foreach (Match m in _regex.Matches(template))
        {
            var nome = m.Groups[1].Value;
            if (!vistas.Add(nome)) continue;

            var info = new VariavelInfo { Nome = nome };
            switch (nome.ToLower())
            {
                case "data":
                    info.ValorPadrao = DateTime.Now.ToString("dd/MM/yyyy");
                    info.AutoPreencher = true;
                    info.Placeholder   = "Data atual";
                    break;
                case "hora":
                    info.ValorPadrao = DateTime.Now.ToString("HH:mm");
                    info.AutoPreencher = true;
                    info.Placeholder   = "Hora atual";
                    break;
                case "datahora":
                    info.ValorPadrao = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    info.AutoPreencher = true;
                    info.Placeholder   = "Data e hora";
                    break;
                case "usuario":
                    info.ValorPadrao = nomeUsuario;
                    info.AutoPreencher = true;
                    info.Placeholder   = "Usuário logado";
                    break;
                case "mes":
                    info.ValorPadrao = DateTime.Now.ToString("MMMM/yyyy");
                    info.AutoPreencher = true;
                    info.Placeholder   = "Mês atual";
                    break;
                case "clipboard":
                    info.ValorPadrao   = ObterClipboard();
                    info.AutoPreencher = true;
                    info.Placeholder   = "Conteúdo da área de transferência";
                    break;
                default:
                    info.Placeholder = $"Valor para {nome}";
                    break;
            }
            lista.Add(info);
        }
        return lista;
    }

    public static string Substituir(string template, Dictionary<string, string> valores)
        => _regex.Replace(template, m =>
            valores.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

    public static bool TemVariaveis(string template) => _regex.IsMatch(template);

    private static string ObterClipboard()
    {
#if WINDOWS
        try { return System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : string.Empty; }
        catch { return string.Empty; }
#else
        return string.Empty;
#endif
    }
}
