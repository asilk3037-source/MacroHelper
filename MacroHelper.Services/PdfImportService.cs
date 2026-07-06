using System.Text;
using UglyToad.PdfPig;

namespace MacroHelper.Services;

public class PdfImportService
{
    public string ExtrairTexto(string caminhoArquivo)
    {
        using var pdf = PdfDocument.Open(caminhoArquivo);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
            sb.Append(page.Text).Append(' ');
        return sb.ToString();
    }
}
