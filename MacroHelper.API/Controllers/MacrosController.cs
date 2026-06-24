using MacroHelper.API.DTOs;
using MacroHelper.API.Middleware;
using MacroHelper.Core.Entities;
using MacroHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MacroHelper.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MacrosController : ControllerBase
{
    private readonly MacroService        _svc;
    private readonly IaService           _ia;
    private readonly IHubContext<SyncHub> _hub;

    public MacrosController(MacroService svc, IaService ia, IHubContext<SyncHub> hub)
    {
        _svc = svc;
        _ia  = ia;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? busca, [FromQuery] string? categoria)
    {
        IEnumerable<Macro> macros;
        if (!string.IsNullOrWhiteSpace(busca))
            macros = await _svc.PesquisarAsync(busca);
        else if (!string.IsNullOrWhiteSpace(categoria))
            macros = await _svc.ObterPorCategoriaAsync(categoria);
        else
            macros = await _svc.ObterTodosAsync();

        return Ok(macros.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var m = await _svc.ObterPorIdAsync(id);
        return m == null ? NotFound() : Ok(ToDto(m));
    }

    [HttpPost]
    public async Task<IActionResult> Salvar([FromBody] SalvarMacroRequest req)
    {
        var macro = new Macro
        {
            Id = req.Id, Atalho = req.Atalho, Titulo = req.Titulo,
            Conteudo = req.Conteudo, Categoria = req.Categoria,
            CategoriaId = req.CategoriaId, Ativo = req.Ativo
        };
        var (ok, msg, saved) = await _svc.SalvarAsync(macro);
        if (!ok) return BadRequest(new { erro = msg });
        await _hub.Clients.All.SendAsync("MacrosAtualizadas");
        return Ok(ToDto(saved!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var (ok, msg) = await _svc.ExcluirAsync(id);
        if (!ok) return NotFound(new { erro = msg });
        await _hub.Clients.All.SendAsync("MacrosAtualizadas");
        return Ok(new { mensagem = msg });
    }

    [HttpPost("gerar")]
    public async Task<IActionResult> GerarComIA([FromBody] GerarMacroRequest req)
    {
        var conteudo = await _ia.GerarConteudoMacroAsync(req.Descricao, req.Tom);
        return Ok(new { conteudo });
    }

    [HttpPost("{id}/ajustar-tom")]
    public async Task<IActionResult> AjustarTom(int id, [FromBody] AjustarTomRequest req)
    {
        var macro = await _svc.ObterPorIdAsync(id);
        if (macro == null) return NotFound();
        var novo = await _ia.AjustarTomAsync(macro.Conteudo, req.Tom);
        return Ok(new { conteudo = novo });
    }

    private static MacroDto ToDto(Macro m) =>
        new(m.Id, m.Atalho, m.Titulo, m.Conteudo, m.Categoria, m.CategoriaId, m.Ativo, m.DataCriacao);
}

public record AjustarTomRequest(string Tom);
