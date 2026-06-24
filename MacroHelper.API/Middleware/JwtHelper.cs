using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MacroHelper.Core.Entities;
using Microsoft.IdentityModel.Tokens;

namespace MacroHelper.API.Middleware;

public static class JwtHelper
{
    public static string GerarToken(Usuario usuario, IConfiguration config)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(30);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name,            usuario.Nome),
            new Claim(ClaimTypes.Email,           usuario.Email),
            new Claim(ClaimTypes.Role,            usuario.Perfil)
        };

        var token = new JwtSecurityToken(
            issuer:   config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims:   claims,
            expires:  expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static int? ObterUsuarioId(ClaimsPrincipal user)
    {
        var val = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(val, out var id) ? id : null;
    }
}
