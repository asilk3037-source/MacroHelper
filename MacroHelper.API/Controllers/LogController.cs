using MacroHelper.API.Middleware;
using MacroHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MacroHelper.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogController : ControllerBase
{
    private readonly LogUsoService _svc;
    public LogController(LogUsoService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate)
    {
        var inicio = de  ?? DateTime.Today.AddDays(-30);
        var fim    = ate ?? DateTime.Today.AddDays(1);
        var lista  = await _svc.ObterPorPeriodoAsync(inicio, fim);
        return Ok(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarLogRequest req)
    {
        var uid = JwtHelper.ObterUsuarioId(User);
        await _svc.RegistrarAsync(req.MacroId, req.MacroTitulo, req.MacroAtalho, uid);
        return Ok();
    }
}

public record RegistrarLogRequest(int? MacroId, string MacroTitulo, string MacroAtalho, string? Aplicativo);
