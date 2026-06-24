using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using Brush = System.Windows.Media.Brush;

namespace MacroHelper.UI.Helpers;

/// <summary>Renderiza um preview formatado leve: **negrito**, *itálico* e {{placeholders}} destacados.</summary>
public static class PreviewFormatador
{
    private static readonly Regex _tokenRegex = new(@"(\*\*[^*]+\*\*|\*[^*]+\*|\{\{[^}]*\}\})", RegexOptions.Compiled);

    public static void Renderizar(TextBlock destino, string conteudo, Brush corDestaque)
    {
        destino.Inlines.Clear();
        var linhas = conteudo.Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < linhas.Length; i++)
        {
            RenderizarLinha(destino, linhas[i], corDestaque);
            if (i < linhas.Length - 1) destino.Inlines.Add(new LineBreak());
        }
    }

    private static void RenderizarLinha(TextBlock destino, string linha, Brush corDestaque)
    {
        var pos = 0;
        foreach (Match m in _tokenRegex.Matches(linha))
        {
            if (m.Index > pos)
                destino.Inlines.Add(new Run(linha[pos..m.Index]));

            var token = m.Value;
            if (token.StartsWith("**"))
                destino.Inlines.Add(new Bold(new Run(token[2..^2])));
            else if (token.StartsWith("{{"))
                destino.Inlines.Add(new Run(token) { Foreground = corDestaque, FontWeight = System.Windows.FontWeights.SemiBold });
            else
                destino.Inlines.Add(new Italic(new Run(token[1..^1])));

            pos = m.Index + m.Length;
        }

        if (pos < linha.Length)
            destino.Inlines.Add(new Run(linha[pos..]));
    }
}
