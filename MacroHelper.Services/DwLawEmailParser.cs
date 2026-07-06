using System.Text.RegularExpressions;
using MacroHelper.Core.Entities;

namespace MacroHelper.Services;

public static class DwLawEmailParser
{
    // SISTEMA patterns: "TRF1 - PJE - 2ª instância", "TJMG - EPROC - 1ª instância", etc.
    private static readonly Regex SistemaRegex = new(
        @"(TRF\s*\d+|TJ[A-Z]{2}|STJ|STF|TRT\s*\d*)\s*[-–]\s*(PJE[\w\s\.]*|EPROC|e-proc)[\s\w\.]*[-–]\s*\d+[aªº]\s*inst[aâ]ncia",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex UrlRegex = new(@"https?://\S+", RegexOptions.Compiled);

    public static List<IntimacaoErro> Parse(string text, DateTime dataEmail)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var result = text.Contains('\t')
            ? ParseTabSeparated(text, dataEmail)
            : ParseUrlAnchored(text, dataEmail);

        return result;
    }

    // ── Strategy 1: direct copy from Outlook (tab-separated) ──────────────
    private static List<IntimacaoErro> ParseTabSeparated(string text, DateTime dataEmail)
    {
        var result = new List<IntimacaoErro>();
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length < 5) continue;
            if (parts[0].Trim().Equals("ADVOGADO", StringComparison.OrdinalIgnoreCase)) continue;

            result.Add(Build(
                dataEmail,
                parts[0].Trim(),
                parts[1].Trim(),
                parts[2].Trim(),
                parts[3].Trim(),
                string.Join(" ", parts.Skip(4)).Trim()
            ));
        }
        return result;
    }

    // ── Strategy 2: free text / PDF copy (URL-anchored) ───────────────────
    private static List<IntimacaoErro> ParseUrlAnchored(string text, DateTime dataEmail)
    {
        // Normalize: collapse line breaks into spaces, squash multiple spaces
        var normalized = Regex.Replace(text, @"[\r\n]+", " ");
        normalized = Regex.Replace(normalized, @"\s{2,}", " ").Trim();

        // Remove known non-data lines
        normalized = Regex.Replace(normalized,
            @"ADVOGADO\s+TIPO\s+DE\s+LOGIN\s+SISTEMA\s+LINK\s+DO\s+SITE\s+DO\s+TRIBUNAL\s+ERRO",
            "", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized,
            @"Prezado\s+cliente.+?DW\s+LAW\.",
            "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        normalized = Regex.Replace(normalized,
            @"Mensagem\s+apenas\s+para.+$",
            "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        normalized = Regex.Replace(normalized,
            @"Para\s+ajudar\s+a\s+proteger.+$",
            "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Find all (SISTEMA, URL) anchors
        var sistemaMatches = SistemaRegex.Matches(normalized).Cast<Match>().ToList();
        var urlMatches = UrlRegex.Matches(normalized).Cast<Match>().ToList();

        if (urlMatches.Count == 0) return [];

        // Pair each URL with the nearest preceding SISTEMA
        var pairs = new List<(Match sistema, Match url)>();
        foreach (var url in urlMatches)
        {
            var prec = sistemaMatches
                .Where(s => s.Index < url.Index)
                .OrderByDescending(s => s.Index)
                .FirstOrDefault();
            if (prec != null)
                pairs.Add((prec, url));
        }

        if (pairs.Count == 0) return [];

        var result = new List<IntimacaoErro>();
        for (int i = 0; i < pairs.Count; i++)
        {
            var (sistemaM, urlM) = pairs[i];

            // Pre-SISTEMA text: from end of previous row's ERRO (or text start) to sistemaStart
            int preStart = (i == 0) ? 0 : pairs[i - 1].url.Index + pairs[i - 1].url.Length;
            var preSystema = normalized.Substring(preStart, sistemaM.Index - preStart).Trim();

            // Split the pre-SISTEMA block into ADVOGADO + TIPO
            (var adv, var tipo) = SplitAdvTipo(preSystema);

            // Post-URL text (ERRO): from urlEnd to start of next SISTEMA
            int urlEnd = urlM.Index + urlM.Length;
            int erroEnd = (i < pairs.Count - 1) ? pairs[i + 1].sistema.Index : normalized.Length;
            var erroRaw = normalized.Substring(urlEnd, erroEnd - urlEnd).Trim();

            // Trim off what belongs to the NEXT row's ADVOGADO (title-case name pattern)
            var nextAdvMatch = Regex.Match(erroRaw,
                @"\b[A-ZÁÉÍÓÚÂÊÎÔÛÃÕ][a-záéíóúâêîôûãõ]+(?:\s+(?:de|da|do|dos|das)\s+)?(?:\s+[A-ZÁÉÍÓÚÂÊÎÔÛÃÕ][a-záéíóúâêîôûãõ]+){1,4}\s*-\s*[A-Z]{2,}\b");
            var erro = nextAdvMatch.Success
                ? erroRaw.Substring(0, nextAdvMatch.Index).Trim()
                : erroRaw;

            if (!string.IsNullOrWhiteSpace(adv) || !string.IsNullOrWhiteSpace(sistemaM.Value))
                result.Add(Build(dataEmail, adv, tipo, sistemaM.Value.Trim(), urlM.Value, erro));
        }

        return result;
    }

    // Splits "Bruno Wurmbauer Junior - EBSERH Procuradoria - ..." into
    // ("Bruno Wurmbauer Junior - EBSERH", "Procuradoria - ...")
    // Relies on ADVOGADO always ending in "- ALLCAPS"
    private static (string adv, string tipo) SplitAdvTipo(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return ("", "");

        // Find the FIRST occurrence of " - ALLCAPS_WORD" — that's where ADVOGADO ends
        var m = Regex.Match(text, @"\s+-\s+([A-ZÁÉÍÓÚÂÊÎÔÛÃÕ]{2,})\b");
        if (!m.Success) return (text.Trim(), "");

        var endIdx = m.Index + m.Length;
        return (text[..endIdx].Trim(), text[endIdx..].Trim());
    }

    private static IntimacaoErro Build(DateTime dataEmail, string adv, string tipo,
        string sistema, string link, string erro) => new()
    {
        DataEmail    = dataEmail.Date,
        Advogado     = adv,
        TipoLogin    = tipo,
        Sistema      = sistema,
        LinkTribunal = link,
        Erro         = NormalizeErro(erro),
        Status       = "Novo",
        CriadoEm    = DateTime.Now,
        AtualizadoEm = DateTime.Now,
    };

    // Normalize verbose error text to a clean short form
    private static string NormalizeErro(string erro)
    {
        erro = erro.Trim();
        if (Regex.IsMatch(erro, @"Login.{0,5}Senha\s+incorretos", RegexOptions.IgnoreCase))
            return "Login/Senha incorretos";
        if (Regex.IsMatch(erro, @"FALHA\s+AO\s+INSTALAR\s+CERTIFICADO", RegexOptions.IgnoreCase))
            return "Falha ao instalar certificado";
        if (Regex.IsMatch(erro, @"Chave\s+MFA", RegexOptions.IgnoreCase))
            return "Chave MFA inválida";
        if (Regex.IsMatch(erro, @"certificado\s+digital", RegexOptions.IgnoreCase))
            return "Certificado digital inválido";
        if (Regex.IsMatch(erro, @"token", RegexOptions.IgnoreCase))
            return "Token inválido/expirado";
        // Return first sentence if text is too long
        if (erro.Length > 120)
        {
            var idx = erro.IndexOf('.', 30);
            return idx > 0 ? erro[..(idx + 1)].Trim() : erro[..120].Trim() + "…";
        }
        return erro;
    }
}
