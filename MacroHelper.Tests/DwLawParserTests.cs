using MacroHelper.Services;

namespace MacroHelper.Tests;

/// <summary>
/// Testa o parser de email da DW LAW — sem banco, sem autenticação, 100% unitário.
/// Cobre os dois formatos de entrada:
///   1) Tab-separado  → cópia direta da tabela no Outlook
///   2) Texto livre   → cópia de PDF ou corpo do email sem formatação
/// </summary>
public class DwLawParserTests
{
    private static readonly DateTime DataTeste = new(2026, 7, 3);

    // ── helpers de asserção ──────────────────────────────────────────────────
    private static void AssertItem(
        MacroHelper.Core.Entities.IntimacaoErro item,
        string advogado, string sistema, string erroContains,
        string? tipoLogin = null)
    {
        Assert.Equal(DataTeste.Date, item.DataEmail.Date);
        Assert.Contains(advogado, item.Advogado, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(sistema,  item.Sistema,  StringComparison.OrdinalIgnoreCase);
        Assert.Contains(erroContains, item.Erro, StringComparison.OrdinalIgnoreCase);
        if (tipoLogin != null)
            Assert.Contains(tipoLogin, item.TipoLogin, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Novo", item.Status);
    }

    // ════════════════════════════════════════════════════════════════════════
    // FORMATO 1 — TAB SEPARADO (cópia direta do Outlook)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_TabSeparado_UmaLinha_RetornaUmItem()
    {
        var texto = "ADVOGADO\tTIPO DE LOGIN\tSISTEMA\tLINK DO SITE DO TRIBUNAL\tERRO\n" +
                    "Bruno Wurmbauer Junior - EBSERH\tEBSERH\tTRF5 - PJE V1.0 - 2ª instância\t" +
                    "https://pje.trf5.jus.br/pje/login.seam\t_FALHA AO INSTALAR CERTIFICADO";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        AssertItem(result[0], "Bruno Wurmbauer Junior - EBSERH", "TRF5", "certificado", "EBSERH");
    }

    [Fact]
    public void Parse_TabSeparado_MultiplasLinhas_RetornaTodosItens()
    {
        var texto =
            "ADVOGADO\tTIPO DE LOGIN\tSISTEMA\tLINK DO SITE DO TRIBUNAL\tERRO\n" +
            "Bruno Wurmbauer Junior - EBSERH\tEBSERH\tTRF5 - PJE V1.0 - 2ª instância\t" +
                "https://pje.trf5.jus.br/pje/login.seam\t_FALHA AO INSTALAR CERTIFICADO\n" +
            "Gabriela de Magalhães Silva - BDMG\tMG073945\tTJMG - EPROC - 1ª instância\t" +
                "https://eproc1g.tjmg.jus.br/\tLogin/Senha incorretos. Por gentileza, providencie a correção junto ao nosso sistema.\n" +
            "Luis Felipe Pires Alves - BDMG\tMG062009\tTJMG - EPROC - 2ª instância\t" +
                "https://eproc2g.tjmg.jus.br/\tChave MFA utilizada na autenticação de dois fatores para login está inválida.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Equal(3, result.Count);
        AssertItem(result[0], "Bruno Wurmbauer Junior - EBSERH", "TRF5", "certificado");
        AssertItem(result[1], "Gabriela de Magalhães Silva - BDMG", "TJMG", "incorretos");
        AssertItem(result[2], "Luis Felipe Pires Alves - BDMG", "TJMG", "MFA");
    }

    [Fact]
    public void Parse_TabSeparado_IgnoraLinhaDeCabecalho()
    {
        var texto =
            "ADVOGADO\tTIPO DE LOGIN\tSISTEMA\tLINK DO SITE DO TRIBUNAL\tERRO\n" +
            "Givaldo Barbosa Macedo Junior - EBSERH\tADVOGADO\tTRF3 - PJE - 1ª instância\t" +
                "https://pje1g.trf3.jus.br/pje/login.seam\tLogin/Senha incorretos.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        // Apenas 1 linha de dados (o cabeçalho não deve aparecer)
        Assert.Single(result);
        AssertItem(result[0], "Givaldo", "TRF3", "incorretos", "ADVOGADO");
    }

    [Fact]
    public void Parse_TabSeparado_LinhaIncompleta_EIgnorada()
    {
        var texto =
            "ADVOGADO\tTIPO DE LOGIN\tSISTEMA\n" +          // só 3 colunas — ignora
            "Givaldo Barbosa Macedo Junior - EBSERH\tADVOGADO\tTRF3 - PJE - 1ª instância\t" +
                "https://pje1g.trf3.jus.br/pje/login.seam\tLogin/Senha incorretos.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
    }

    [Fact]
    public void Parse_TabSeparado_DataEmailCorreta()
    {
        var data = new DateTime(2026, 7, 6);
        var texto =
            "Bruno Wurmbauer Junior - EBSERH\tEBSERH\tTRF5 - PJE V1.0 - 2ª instância\t" +
            "https://pje.trf5.jus.br/pje/login.seam\t_FALHA AO INSTALAR CERTIFICADO";

        var result = DwLawEmailParser.Parse(texto, data);

        Assert.Equal(data.Date, result[0].DataEmail.Date);
    }

    // ════════════════════════════════════════════════════════════════════════
    // FORMATO 2 — TEXTO LIVRE / URL-ANCHORED (PDF ou corpo sem formatação)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_TextoLivre_UmItem_ExtracaoCorreta()
    {
        var texto =
            "Bruno Wurmbauer Junior - EBSERH EBSERH " +
            "TRF5 - PJE V1.0 - 2ª instância https://pje.trf5.jus.br/pje/login.seam " +
            "_FALHA AO INSTALAR CERTIFICADO";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        Assert.Contains("TRF5", result[0].Sistema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("certificado", result[0].Erro, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://pje.trf5.jus.br/pje/login.seam", result[0].LinkTribunal);
    }

    [Fact]
    public void Parse_TextoLivre_MultiplosSistemas_RetornaVarioItens()
    {
        var texto =
            "Gabriela de Magalhães Silva - BDMG MG073945 " +
            "TJMG - EPROC - 1ª instância https://eproc1g.tjmg.jus.br/ " +
            "Login/Senha incorretos. Por gentileza, providencie a correção junto ao nosso sistema. " +
            "Gabriela de Magalhães Silva - BDMG MG073945 " +
            "TJMG - EPROC - 2ª instância https://eproc2g.tjmg.jus.br/ " +
            "Login/Senha incorretos. Por gentileza, providencie a correção junto ao nosso sistema.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Contains("incorretos", r.Erro, StringComparison.OrdinalIgnoreCase));
        Assert.All(result, r => Assert.Contains("TJMG",       r.Sistema, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_TextoLivre_ComCabecalhoCompleto_IgnoraCabecalho()
    {
        var texto =
            "ADVOGADO TIPO DE LOGIN SISTEMA LINK DO SITE DO TRIBUNAL ERRO " +
            "Luis Felipe Pires Alves - BDMG MG062009 " +
            "TJMG - EPROC - 1ª instância https://eproc1g.tjmg.jus.br/ " +
            "Chave MFA utilizada na autenticação de dois fatores para login está inválida. Gentileza verificar.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        AssertItem(result[0], "Luis Felipe", "TJMG", "MFA");
    }

    [Fact]
    public void Parse_TextoLivre_TrfEPjeReconhecidos()
    {
        var texto =
            "Givaldo Barbosa Macedo Junior - EBSERH ADVOGADO " +
            "TRF3 - PJE - 1ª instância https://pje1g.trf3.jus.br/pje/login.seam " +
            "Login/Senha incorretos.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        Assert.Contains("TRF3", result[0].Sistema);
    }

    // ════════════════════════════════════════════════════════════════════════
    // NORMALIZAÇÃO DE ERROS
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("_FALHA AO INSTALAR CERTIFICADO",                      "certificado")]
    [InlineData("Login/Senha incorretos. Por gentileza...",             "incorretos")]
    [InlineData("Chave MFA utilizada na autenticação... inválida.",     "MFA")]
    public void Parse_TabSeparado_NormalizaErrosConhecidos(string erroRaw, string erroEsperado)
    {
        var texto =
            $"Bruno Wurmbauer Junior - EBSERH\tEBSERH\tTRF5 - PJE V1.0 - 2ª instância\t" +
            $"https://pje.trf5.jus.br/pje/login.seam\t{erroRaw}";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        Assert.Contains(erroEsperado, result[0].Erro, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS LIMÍTROFES / ROBUSTEZ
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_TextoVazio_RetornaListaVazia()
    {
        Assert.Empty(DwLawEmailParser.Parse("", DataTeste));
        Assert.Empty(DwLawEmailParser.Parse("   ", DataTeste));
    }

    [Fact]
    public void Parse_TextoSemUrl_RetornaListaVazia()
    {
        var texto = "Bruno Wurmbauer Junior - EBSERH EBSERH TRF5 - PJE V1.0 - 2ª instância Login/Senha incorretos.";
        Assert.Empty(DwLawEmailParser.Parse(texto, DataTeste));
    }

    [Fact]
    public void Parse_TextoComRodapeExtraido_NaoContaminaItens()
    {
        var texto =
            "Luis Felipe Pires Alves - BDMG MG062009 " +
            "TJMG - EPROC - 2ª instância https://eproc2g.tjmg.jus.br/ " +
            "Chave MFA inválida. Gentileza verificar. " +
            "Para ajudar a proteger a sua privacidade, o Microsoft Office impediu o download automático desta imagem. " +
            "DW LAW Mensagem apenas para o destinatário pretendido.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        Assert.DoesNotContain("Microsoft", result[0].Erro);
        Assert.DoesNotContain("privacidade", result[0].Erro);
    }

    [Fact]
    public void Parse_StatusSempreNovo_QuandoExtraido()
    {
        var texto =
            "Bruno Wurmbauer Junior - EBSERH\tEBSERH\tTRF5 - PJE V1.0 - 2ª instância\t" +
            "https://pje.trf5.jus.br/pje/login.seam\t_FALHA AO INSTALAR CERTIFICADO";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.All(result, r => Assert.Equal("Novo", r.Status));
    }

    [Fact]
    public void Parse_LinkTribunalPreservado_Exatamente()
    {
        const string url = "https://pje2g.trf1.jus.br/pje/login.seam";
        var texto =
            $"Bruno Wurmbauer Junior - EBSERH\tProcuradoria - EBSERH\t" +
            $"TRF1 - PJE - 2ª instância\t{url}\tLogin/Senha incorretos.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        Assert.Equal(url, result[0].LinkTribunal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CENÁRIOS DE EXTRAÇÃO VIA PDF (page.Text — texto livre com cabeçalho)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_PDF_CabecalhoEmailCompleto_IgnoradoNoPrimeiroItem()
    {
        // Simula o output de page.Text de um PDF de email DW LAW:
        // cabeçalho de email completo → preamble → dados da tabela
        var texto =
            "Aline Martins | Suporte Netview " +
            "De: Painel de Intimações - DW LAW <painel.intimacoes@dwlaw.com.br> " +
            "Enviado em: sexta-feira, 3 de julho de 2026 16:55 " +
            "Para: Aline Martins | Suporte Netview <aline@netview.com.br> " +
            "Assunto: PROBLEMAS NO ACESSO AO PAINEL DW LAW " +
            "Segue abaixo os sistemas onde identificamos problemas de acesso ao Painel: " +
            "Bruno Wurmbauer Junior - EBSERH EBSERH " +
            "TRF5 - PJE V1.0 - 2ª instância https://pje.trf5.jus.br/pje/login.seam " +
            "_FALHA AO INSTALAR CERTIFICADO " +
            "Gabriela de Magalhães Silva - BDMG MG073945 " +
            "TJMG - EPROC - 1ª instância https://eproc1g.tjmg.jus.br/ " +
            "Login/Senha incorretos.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        // Deve extrair exatamente 2 itens (não mais o cabeçalho como linha extra)
        Assert.Equal(2, result.Count);
        Assert.Contains("Bruno Wurmbauer Junior", result[0].Advogado);
        Assert.Contains("TRF5", result[0].Sistema);
        Assert.Contains("Gabriela de Magalhães Silva", result[1].Advogado);
        Assert.Contains("TJMG", result[1].Sistema);
    }

    [Fact]
    public void Parse_PDF_UrlNaoTribunal_EIgnorada()
    {
        // URL do painel DW LAW (não jus.br) deve ser ignorada;
        // apenas URLs de tribunais (*.jus.br) são consideradas âncoras de linha.
        var texto =
            "Acesse o painel em https://painel.intimacoes.dwlaw.com.br para mais detalhes. " +
            "Bruno Wurmbauer Junior - EBSERH EBSERH " +
            "TRF5 - PJE V1.0 - 2ª instância https://pje.trf5.jus.br/pje/login.seam " +
            "_FALHA AO INSTALAR CERTIFICADO";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        // Apenas 1 item — a URL do painel DW LAW não gera linha de dados
        Assert.Single(result);
        Assert.Equal("https://pje.trf5.jus.br/pje/login.seam", result[0].LinkTribunal);
        Assert.Contains("Bruno Wurmbauer Junior", result[0].Advogado);
    }

    [Fact]
    public void Parse_PDF_AdvogadoCorreto_MesmoComLixoAntes()
    {
        // SplitAdvTipo deve usar o ÚLTIMO padrão "- EMPRESA" (mais próximo do SISTEMA),
        // ignorando padrões anteriores do cabeçalho ("- DW", "- LAW", etc.)
        var texto =
            "De: Painel de Intimações - DW LAW <painel@dwlaw.com.br> " +
            "Luis Felipe Pires Alves - BDMG MG062009 " +
            "TJMG - EPROC - 2ª instância https://eproc2g.tjmg.jus.br/ " +
            "Chave MFA inválida.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Single(result);
        Assert.Equal("Luis Felipe Pires Alves - BDMG", result[0].Advogado);
        Assert.Equal("MG062009", result[0].TipoLogin.Trim());
    }

    [Fact]
    public void Parse_PDF_MultiplasLinhas_OrdemCorreta()
    {
        var texto =
            "De: Painel - DW LAW <painel@dwlaw.com.br> Assunto: PROBLEMAS " +
            "Givaldo Barbosa Macedo Junior - EBSERH ADVOGADO " +
            "TRF3 - PJE - 1ª instância https://pje1g.trf3.jus.br/pje/login.seam " +
            "Login/Senha incorretos. " +
            "Luis Felipe Pires Alves - BDMG MG062009 " +
            "TJMG - EPROC - 1ª instância https://eproc1g.tjmg.jus.br/ " +
            "Chave MFA inválida.";

        var result = DwLawEmailParser.Parse(texto, DataTeste);

        Assert.Equal(2, result.Count);
        Assert.Contains("Givaldo", result[0].Advogado);
        Assert.Contains("TRF3", result[0].Sistema);
        Assert.Contains("incorretos", result[0].Erro);
        Assert.Contains("Luis Felipe", result[1].Advogado);
        Assert.Contains("TJMG", result[1].Sistema);
        Assert.Contains("MFA", result[1].Erro);
    }
}
