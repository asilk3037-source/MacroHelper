namespace MacroHelper.API.DTOs;

public record LoginRequest(string Email, string Senha);
public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Confirmar, string Perfil = "Usuario");
public record AuthResponse(string Token, string Nome, string Perfil, int Id);
