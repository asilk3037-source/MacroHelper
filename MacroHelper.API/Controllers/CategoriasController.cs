using MacroHelper.API.DTOs;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MacroHelper.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly CategoriaService _svc;
    public CategoriasController(CategoriaService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var arvore = await _svc.ObterArvoreAsync();
        return Ok(arvore.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Salvar([FromBody] SalvarCategoriaRequest req)
    {
        var cat = new Categoria
        {
            Id = req.Id, Nome = req.Nome, Icone = req.Icone,
            Cor = req.Cor, PaiId = req.PaiId, Ordem = req.Ordem
        };
        var (ok, msg) = await _svc.SalvarAsync(cat);
        if (!ok) return BadRequest(new { erro = msg });
        return Ok(new { mensagem = msg });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        await _svc.ExcluirAsync(id);
        return Ok();
    }

    private static CategoriaDto ToDto(Categoria c) =>
        new(c.Id, c.Nome, c.Icone, c.Cor, c.PaiId, c.NomePai, c.Ordem,
            c.Subcategorias.Select(ToDto).ToList());
}
