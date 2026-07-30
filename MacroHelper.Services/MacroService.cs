using MacroHelper.Core.Entities;
using MacroHelper.Core.Interfaces;
using MacroHelper.Data.Repositories;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MacroHelper.Services;

public class MacroService
{
    private readonly IMacroRepository      _repository;
    private readonly UsuarioService        _usuarioService;
    private readonly MacroVersaoRepository? _versaoRepo;
    private readonly LogAuditoriaRepository? _auditoriaRepo;
    private readonly FavoritoUsuarioRepository? _favoritoUsuarioRepo;
    private readonly VariavelGlobalService? _variavelGlobalService;
    private readonly NotificacaoService? _notificacaoService;
    private static readonly Regex _macroRefRegex = new(@"\{macro:([\w-]+)\}", RegexOptions.Compiled);
    private static readonly Regex _condicionalRegex = new(@"\{se\s+(\w+)=([^}]+)\}(.*?)\{fim\}",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] _diasSemana =
        ["domingo", "segunda", "terca", "quarta", "quinta", "sexta", "sabado"];

    public MacroService(IMacroRepository repository, UsuarioService usuarioService,
        MacroVersaoRepository? versaoRepo = null, LogAuditoriaRepository? auditoriaRepo = null,
        FavoritoUsuarioRepository? favoritoUsuarioRepo = null, VariavelGlobalService? variavelGlobalService = null,
        NotificacaoService? notificacaoService = null)
    {
        _repository           = repository;
        _usuarioService       = usuarioService;
        _versaoRepo           = versaoRepo;
        _auditoriaRepo        = auditoriaRepo;
        _favoritoUsuarioRepo  = favoritoUsuarioRepo;
        _variavelGlobalService = variavelGlobalService;
        _notificacaoService   = notificacaoService;
    }

    private bool EhAdmin => _usuarioService.UsuarioAtual?.Perfil == "Admin";

    private bool TemPermissaoCategoria(Macro macro)
    {
        var u = _usuarioService.UsuarioAtual;
        if (u == null || u.Perfil == "Admin") return true;
        if (!TemPermissaoGrupo(macro, u)) return false;
        if (string.IsNullOrWhiteSpace(u.CategoriasPermitidas)) return true; // sem restrição configurada

        if (macro.CategoriaId == null) return true; // macros sem categoria são sempre visíveis
        var permitidas = u.CategoriasPermitidas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse).ToHashSet();
        return permitidas.Contains(macro.CategoriaId.Value);
    }

    private static bool TemPermissaoGrupo(Macro macro, Usuario u)
    {
        if (string.IsNullOrWhiteSpace(macro.GruposPermitidos)) return true; // sem restrição de grupo
        if (u.GrupoId == null) return false; // macro restrita a grupos, mas usuário não pertence a nenhum

        var grupos = macro.GruposPermitidos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse).ToHashSet();
        return grupos.Contains(u.GrupoId.Value);
    }

    private IEnumerable<Macro> FiltrarPermissao(IEnumerable<Macro> macros) =>
        macros.Where(TemPermissaoCategoria);

    public async Task<IEnumerable<Macro>> ObterTodosAsync() =>
        await MarcarFavoritosPessoaisAsync(FiltrarPermissao(await _repository.GetAllAsync()));

    public async Task<IEnumerable<Macro>> PesquisarAsync(string termo) =>
        await MarcarFavoritosPessoaisAsync(FiltrarPermissao(string.IsNullOrWhiteSpace(termo)
            ? await _repository.GetAllAsync()
            : await _repository.SearchAsync(termo.Trim())));

    public async Task<IEnumerable<Macro>> ObterPorCategoriaAsync(string categoria) =>
        await MarcarFavoritosPessoaisAsync(FiltrarPermissao(await _repository.GetByCategoriaAsync(categoria)));

    /// <summary>Marca Macro.FavoritoPessoal (em memória) de acordo com os favoritos pessoais do usuário logado.</summary>
    private async Task<IEnumerable<Macro>> MarcarFavoritosPessoaisAsync(IEnumerable<Macro> macros)
    {
        var lista = macros.ToList();
        var usuarioId = _usuarioService.UsuarioAtual?.Id;
        if (_favoritoUsuarioRepo == null || usuarioId == null) return lista;

        var ids = await _favoritoUsuarioRepo.ObterIdsAsync(usuarioId.Value);
        foreach (var m in lista) m.FavoritoPessoal = ids.Contains(m.Id);
        return lista;
    }

    /// <summary>Alterna o favorito pessoal do usuário logado para a macro (não afeta outros usuários).</summary>
    public async Task<bool> ToggleFavoritoPessoalAsync(int macroId)
    {
        var usuarioId = _usuarioService.UsuarioAtual?.Id;
        if (_favoritoUsuarioRepo == null || usuarioId == null) return false;
        return await _favoritoUsuarioRepo.AlternarAsync(usuarioId.Value, macroId);
    }

    public async Task<Macro?> ObterPorIdAsync(int id) =>
        await _repository.GetByIdAsync(id);

    public async Task<IEnumerable<string>> ObterCategoriasAsync() =>
        await _repository.GetCategoriasAsync();

    public async Task<IEnumerable<Macro>> ObterFavoritosAsync() =>
        FiltrarPermissao(await _repository.GetFavoritosAsync());

    public async Task<IEnumerable<Macro>> ObterPendentesAsync() => await _repository.GetPendentesAsync();

    public async Task<Macro?> ObterPorAtalhoTeclaAsync(string digito) =>
        await _repository.GetByAtalhoTeclaAsync(digito);

    /// <summary>Destaque global de equipe — visível para todos. Somente admin pode alterar.</summary>
    public async Task ToggleFavoritoAsync(Macro macro)
    {
        if (!EhAdmin) return;
        macro.Favorito = !macro.Favorito;
        await _repository.ToggleFavoritoAsync(macro.Id, macro.Favorito);
    }

    public async Task<(bool Sucesso, string Mensagem)> AprovarAsync(int id)
    {
        var macro = await _repository.GetByIdAsync(id);
        await _repository.AtualizarStatusAsync(id, "Aprovada");
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService.UsuarioAtual?.Id, "Aprovar", "Macro", id, null);
        await NotificarAutorAsync(macro, aprovada: true, motivo: null);
        return (true, "Macro aprovada e publicada.");
    }

    public async Task<(bool Sucesso, string Mensagem)> RejeitarAsync(int id, string? motivo = null)
    {
        var macro = await _repository.GetByIdAsync(id);
        await _repository.DeleteAsync(id);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService.UsuarioAtual?.Id, "Rejeitar", "Macro", id, motivo);
        await NotificarAutorAsync(macro, aprovada: false, motivo);
        return (true, "Macro rejeitada e removida.");
    }

    /// <summary>Notifica o autor da macro (via central de notificações) sobre a decisão de aprovação/rejeição.</summary>
    private async Task NotificarAutorAsync(Macro? macro, bool aprovada, string? motivo)
    {
        if (_notificacaoService == null || macro?.CriadoPorId == null) return;

        var autor = await _usuarioService.ObterPorIdAsync(macro.CriadoPorId.Value);
        if (autor == null) return;

        var titulo = aprovada ? "Macro aprovada" : "Macro rejeitada";
        var mensagem = aprovada
            ? $"{autor.Nome}, sua macro \"{macro.Titulo}\" foi aprovada e já está disponível para a equipe."
            : $"{autor.Nome}, sua macro \"{macro.Titulo}\" foi rejeitada." +
              (string.IsNullOrWhiteSpace(motivo) ? "" : $" Motivo: {motivo}");

        await _notificacaoService.RegistrarAsync(titulo, mensagem, aprovada ? "Sucesso" : "Aviso");
    }

    /// <summary>Procura uma macro existente com conteúdo muito parecido (Jaccard sobre palavras), para alertar antes de salvar.</summary>
    public async Task<Macro?> DetectarPossivelDuplicataAsync(Macro macro)
    {
        const double LIMIAR = 0.75;
        var todas = await _repository.GetAllAsync();
        return todas
            .Where(m => m.Id != macro.Id)
            .Select(m => (Macro: m, Score: SimilaridadeJaccard(macro.Conteudo, m.Conteudo)))
            .Where(t => t.Score >= LIMIAR)
            .OrderByDescending(t => t.Score)
            .Select(t => t.Macro)
            .FirstOrDefault();
    }

    private static double SimilaridadeJaccard(string a, string b)
    {
        var separadores = new[] { ' ', '\n', '\r', '\t', '.', ',', ';' };
        var wa = a.ToLowerInvariant().Split(separadores, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var wb = b.ToLowerInvariant().Split(separadores, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (wa.Count == 0 || wb.Count == 0) return 0;
        var intersecao = wa.Intersect(wb).Count();
        var uniao      = wa.Union(wb).Count();
        return uniao == 0 ? 0 : (double)intersecao / uniao;
    }

    public async Task<(bool Sucesso, string Mensagem, Macro? Macro)> SalvarAsync(Macro macro)
    {
        if (string.IsNullOrWhiteSpace(macro.Atalho))
            return (false, "O atalho é obrigatório.", null);

        if (string.IsNullOrWhiteSpace(macro.Titulo))
            return (false, "O título é obrigatório.", null);

        if (string.IsNullOrWhiteSpace(macro.Conteudo))
            return (false, "O conteúdo é obrigatório.", null);

        macro.Atalho = macro.Atalho.Trim().ToLower().Replace(" ", "-");

        // Usuários comuns entram em fluxo de aprovação; admins publicam direto.
        if (!EhAdmin)
        {
            macro.Status      = "Pendente";
            macro.CriadoPorId = _usuarioService.UsuarioAtual?.Id;
        }
        else if (string.IsNullOrWhiteSpace(macro.Status))
        {
            macro.Status = "Aprovada";
        }

        if (macro.Id == 0)
        {
            var id = await _repository.InsertAsync(macro);
            macro.Id = id;
            if (_auditoriaRepo != null)
                await _auditoriaRepo.RegistrarAsync(_usuarioService.UsuarioAtual?.Id, "Criar", "Macro", macro.Id, $"Atalho: {macro.Atalho}");
            var msg = macro.Status == "Pendente" ? "Macro enviada para aprovação!" : "Macro criada com sucesso!";
            return (true, msg, macro);
        }

        var anterior = await _repository.GetByIdAsync(macro.Id);
        if (anterior != null && _versaoRepo != null)
            await _versaoRepo.RegistrarAsync(macro.Id, anterior.Titulo, anterior.Conteudo, _usuarioService.UsuarioAtual?.Id);

        await _repository.UpdateAsync(macro);

        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService.UsuarioAtual?.Id, "Editar", "Macro", macro.Id, $"Atalho: {macro.Atalho}");

        var msgUpd = macro.Status == "Pendente" ? "Macro atualizada e enviada para aprovação!" : "Macro atualizada com sucesso!";
        return (true, msgUpd, macro);
    }

    public async Task<IEnumerable<MacroVersao>> ObterVersoesAsync(int macroId) =>
        _versaoRepo == null ? [] : await _versaoRepo.GetByMacroAsync(macroId);

    /// <summary>Arquivamento reversível (soft-archive): a macro deixa de aparecer em buscas, atalhos,
    /// favoritos e comunidade, mas continua existindo e pode ser restaurada — diferente de ExcluirAsync.</summary>
    public async Task<(bool Sucesso, string Mensagem)> ArquivarAsync(int id, bool arquivar)
    {
        await _repository.ToggleAtivoAsync(id, !arquivar);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService.UsuarioAtual?.Id,
                arquivar ? "Arquivar" : "Desarquivar", "Macro", id, null);
        return (true, arquivar ? "Macro arquivada." : "Macro restaurada.");
    }

    public async Task<(bool Sucesso, string Mensagem)> ExcluirAsync(int id)
    {
        var macro = await _repository.GetByIdAsync(id);
        if (macro == null)
            return (false, "Macro não encontrada.");

        await _repository.DeleteAsync(id);
        if (_auditoriaRepo != null)
            await _auditoriaRepo.RegistrarAsync(_usuarioService.UsuarioAtual?.Id, "Excluir", "Macro", id, $"Atalho: {macro.Atalho}");
        return (true, $"Macro \"{macro.Titulo}\" excluída com sucesso.");
    }

    public async Task<IEnumerable<Macro>> BuscarPorGatilhoAsync(string texto, char prefixo = '/')
    {
        if (texto.Length < 2 || texto[0] != prefixo)
            return [];

        var termo = texto[1..];
        return FiltrarPermissao(await _repository.SearchAsync(termo));
    }

    /// <summary>Resolve referências {macro:atalho-x} dentro do conteúdo, expandindo macros aninhadas (até 3 níveis),
    /// e em seguida substitui placeholders de Variáveis Globais cadastradas (ex: {assinatura}).</summary>
    public async Task<string> ResolverMacrosAninhadasAsync(string conteudo, int profundidade = 0)
    {
        if (profundidade < 3 && _macroRefRegex.IsMatch(conteudo))
        {
            var matches = _macroRefRegex.Matches(conteudo);
            foreach (Match m in matches)
            {
                var atalho = m.Groups[1].Value;
                var referenciada = await _repository.GetByAtalhoAsync(atalho);
                var valor = referenciada != null
                    ? await ResolverMacrosAninhadasAsync(referenciada.Conteudo, profundidade + 1)
                    : string.Empty;
                conteudo = conteudo.Replace(m.Value, valor);
            }
        }

        if (profundidade == 0 && _variavelGlobalService != null)
            conteudo = await _variavelGlobalService.ResolverAsync(conteudo);

        return conteudo;
    }

    /// <summary>Resolve blocos {se campo=valor}...{fim} de acordo com a categoria da macro, usuário logado e dia da semana.</summary>
    public string ResolverCondicionais(string conteudo, Macro macro)
    {
        var contexto = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["categoria"] = macro.Categoria ?? string.Empty,
            ["usuario"]   = _usuarioService.UsuarioAtual?.Nome ?? string.Empty,
            ["diasemana"] = _diasSemana[(int)DateTime.Now.DayOfWeek]
        };

        return _condicionalRegex.Replace(conteudo, m =>
        {
            var campo = m.Groups[1].Value;
            var valorEsperado = m.Groups[2].Value.Trim();
            var bloco = m.Groups[3].Value;
            if (!contexto.TryGetValue(campo, out var valorAtual)) return string.Empty;
            return string.Equals(valorAtual, valorEsperado, StringComparison.OrdinalIgnoreCase) ? bloco : string.Empty;
        });
    }

    // ── Import / Export JSON ──────────────────────────────────────
    private class MacroExportDto
    {
        public string Atalho { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public bool Ativo { get; set; } = true;
    }

    public async Task<string> ExportarJsonAsync()
    {
        var macros = await _repository.GetAllAsync();
        var dtos = macros.Select(m => new MacroExportDto
        {
            Atalho = m.Atalho, Titulo = m.Titulo, Conteudo = m.Conteudo,
            Categoria = m.Categoria, Ativo = m.Ativo
        });
        return JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<(bool Sucesso, string Mensagem, int Importadas, int Ignoradas)> ImportarJsonAsync(string json)
    {
        List<MacroExportDto>? dtos;
        try { dtos = JsonSerializer.Deserialize<List<MacroExportDto>>(json); }
        catch (Exception ex) { return (false, $"JSON inválido: {ex.Message}", 0, 0); }

        if (dtos == null || dtos.Count == 0)
            return (false, "Nenhuma macro encontrada no arquivo.", 0, 0);

        var existentes = (await _repository.GetAllAsync()).Select(m => m.Atalho).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int importadas = 0, ignoradas = 0;

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Atalho) || string.IsNullOrWhiteSpace(dto.Conteudo))
            {
                ignoradas++; continue;
            }
            if (existentes.Contains(dto.Atalho))
            {
                ignoradas++; continue;
            }

            await _repository.InsertAsync(new Macro
            {
                Atalho = dto.Atalho.Trim().ToLower().Replace(" ", "-"),
                Titulo = string.IsNullOrWhiteSpace(dto.Titulo) ? dto.Atalho : dto.Titulo,
                Conteudo = dto.Conteudo,
                Categoria = dto.Categoria,
                Ativo = dto.Ativo,
                Status = "Aprovada"
            });
            existentes.Add(dto.Atalho);
            importadas++;
        }

        return (true, $"{importadas} macro(s) importada(s), {ignoradas} ignorada(s) (duplicadas ou inválidas).", importadas, ignoradas);
    }

    // ── Import CSV (cabeçalho: Atalho,Titulo,Conteudo,Categoria) ──────
    public async Task<(bool Sucesso, string Mensagem, int Importadas, int Ignoradas)> ImportarCsvAsync(string csv)
    {
        List<string[]> linhas;
        try { linhas = ParseCsv(csv); }
        catch (Exception ex) { return (false, $"CSV inválido: {ex.Message}", 0, 0); }

        if (linhas.Count < 2)
            return (false, "Nenhuma macro encontrada. A primeira linha deve ser o cabeçalho (Atalho,Titulo,Conteudo,Categoria).", 0, 0);

        var cabecalho = linhas[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        var idxAtalho    = cabecalho.IndexOf("atalho");
        var idxTitulo    = cabecalho.IndexOf("titulo");
        var idxConteudo  = cabecalho.IndexOf("conteudo");
        var idxCategoria = cabecalho.IndexOf("categoria");

        if (idxAtalho < 0 || idxTitulo < 0 || idxConteudo < 0)
            return (false, "Cabeçalho deve conter as colunas Atalho, Titulo e Conteudo (Categoria é opcional).", 0, 0);

        var existentes = (await _repository.GetAllAsync()).Select(m => m.Atalho).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int importadas = 0, ignoradas = 0;

        for (var i = 1; i < linhas.Count; i++)
        {
            var l = linhas[i];
            if (l.Length == 1 && string.IsNullOrWhiteSpace(l[0])) continue;
            string Col(int idx) => idx >= 0 && idx < l.Length ? l[idx] : string.Empty;

            var atalho   = Col(idxAtalho).Trim();
            var conteudo = Col(idxConteudo);
            if (string.IsNullOrWhiteSpace(atalho) || string.IsNullOrWhiteSpace(conteudo))
            {
                ignoradas++; continue;
            }

            var atalhoNormalizado = atalho.ToLower().Replace(" ", "-");
            if (existentes.Contains(atalhoNormalizado))
            {
                ignoradas++; continue;
            }

            await _repository.InsertAsync(new Macro
            {
                Atalho    = atalhoNormalizado,
                Titulo    = string.IsNullOrWhiteSpace(Col(idxTitulo)) ? atalho : Col(idxTitulo),
                Conteudo  = conteudo,
                Categoria = idxCategoria >= 0 && !string.IsNullOrWhiteSpace(Col(idxCategoria)) ? Col(idxCategoria) : null,
                Ativo     = true,
                Status    = "Aprovada"
            });
            existentes.Add(atalhoNormalizado);
            importadas++;
        }

        return (true, $"{importadas} macro(s) importada(s), {ignoradas} ignorada(s) (duplicadas ou inválidas).", importadas, ignoradas);
    }

    // ── Comunidade interna ──────────────────────────────────────────
    public async Task<IEnumerable<Macro>> ObterPublicasAsync()
    {
        var publicas = (await _repository.GetPublicasAsync()).ToList();
        var usuarioId = _usuarioService.UsuarioAtual?.Id;
        if (usuarioId != null)
        {
            var curtidas = await _repository.ObterCurtidasDoUsuarioAsync(usuarioId.Value);
            foreach (var m in publicas) m.CurtidaPeloUsuario = curtidas.Contains(m.Id);
        }
        return publicas;
    }

    public async Task TogglePublicaAsync(Macro macro)
    {
        macro.Publica = !macro.Publica;
        await _repository.TogglePublicaAsync(macro.Id, macro.Publica);
    }

    public async Task<bool> ToggleCurtidaAsync(int macroId)
    {
        var usuarioId = _usuarioService.UsuarioAtual?.Id;
        if (usuarioId == null) return false;
        return await _repository.ToggleCurtidaAsync(macroId, usuarioId.Value);
    }

    /// <summary>Copia uma macro pública para a biblioteca pessoal do usuário logado (com novo atalho).</summary>
    public async Task<(bool Sucesso, string Mensagem)> CopiarParaMinhasAsync(int macroId)
    {
        var original = await _repository.GetByIdAsync(macroId);
        if (original == null) return (false, "Macro não encontrada.");

        var copia = new Macro
        {
            Atalho   = $"{original.Atalho}-copia",
            Titulo   = $"{original.Titulo} (cópia)",
            Conteudo = original.Conteudo,
            Categoria = original.Categoria,
            CategoriaId = original.CategoriaId,
            Ativo    = true
        };
        var (ok, msg, _) = await SalvarAsync(copia);
        return (ok, ok ? "Macro copiada para sua biblioteca!" : msg);
    }

    /// <summary>Parser CSV simples com suporte a campos entre aspas (vírgulas/quebras de linha/aspas escapadas).</summary>
    private static List<string[]> ParseCsv(string conteudo)
    {
        var linhas = new List<string[]>();
        var campos = new List<string>();
        var campoAtual = new System.Text.StringBuilder();
        var dentroDeAspas = false;
        var i = 0;

        while (i < conteudo.Length)
        {
            var c = conteudo[i];
            if (dentroDeAspas)
            {
                if (c == '"')
                {
                    if (i + 1 < conteudo.Length && conteudo[i + 1] == '"') { campoAtual.Append('"'); i += 2; continue; }
                    dentroDeAspas = false; i++; continue;
                }
                campoAtual.Append(c); i++; continue;
            }

            if (c == '"') { dentroDeAspas = true; i++; continue; }
            if (c == ',') { campos.Add(campoAtual.ToString()); campoAtual.Clear(); i++; continue; }
            if (c == '\r') { i++; continue; }
            if (c == '\n')
            {
                campos.Add(campoAtual.ToString()); campoAtual.Clear();
                linhas.Add(campos.ToArray()); campos = new List<string>();
                i++; continue;
            }
            campoAtual.Append(c); i++;
        }

        if (campoAtual.Length > 0 || campos.Count > 0)
        {
            campos.Add(campoAtual.ToString());
            linhas.Add(campos.ToArray());
        }
        return linhas;
    }
}
