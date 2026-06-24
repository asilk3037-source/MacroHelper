namespace MacroHelper.API.DTOs;

public record CategoriaDto(int Id, string Nome, string? Icone, string? Cor,
    int? PaiId, string? NomePai, int Ordem, List<CategoriaDto> Subcategorias);

public record SalvarCategoriaRequest(string Nome, string? Icone, string? Cor, int? PaiId, int Ordem, int Id = 0);
