using MacroHelper.API.DTOs;
using MacroHelper.API.Middleware;
using MacroHelper.Services;
using Microsoft.AspNetCore.Mvc;

namespace MacroHelper.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UsuarioService _svc;
    private readonly IConfiguration _config;

    public AuthController(UsuarioService svc, IConfiguration config)
    {
        _svc    = svc;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var (ok, msg, user) = await _svc.LoginAsync(req.Email, req.Senha);
        if (!ok) return Unauthorized(new { erro = msg });

        var token = JwtHelper.GerarToken(user!, _config);
        return Ok(new AuthResponse(token, user!.Nome, user.Perfil, user.Id));
    }

    [HttpPost("criar")]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest req)
    {
        var (ok, msg) = await _svc.CriarAsync(req.Nome, req.Email, req.Senha, req.Confirmar, req.Perfil);
        if (!ok) return BadRequest(new { erro = msg });

        var (_, _, user) = await _svc.LoginAsync(req.Email, req.Senha);
        var token = JwtHelper.GerarToken(user!, _config);
        return Ok(new AuthResponse(token, user!.Nome, user.Perfil, user.Id));
    }
}
