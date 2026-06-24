namespace MacroHelper.API.DTOs;

public record MacroDto(int Id, string Atalho, string Titulo, string Conteudo,
    string? Categoria, int? CategoriaId, bool Ativo, DateTime DataCriacao);

public record SalvarMacroRequest(string Atalho, string Titulo, string Conteudo,
    string? Categoria, int? CategoriaId, bool Ativo, int Id = 0);

public record GerarMacroRequest(string Descricao, string? Tom = null);
