using System.Text;
using MacroHelper.API;
using MacroHelper.Data.Context;
using MacroHelper.Data.Repositories;
using MacroHelper.Core.Interfaces;
using MacroHelper.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "SK MacroHelper API",
        Version = "v1",
        Description = "API para sincronização — by Aline Martins · Silk"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" }}, []
    }});
});

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddSignalR();

// Data
builder.Services.AddSingleton<DatabaseContext>();
builder.Services.AddSingleton<SupabaseContext>();
builder.Services.AddSingleton<IMacroRepository, MacroRepository>();
builder.Services.AddSingleton<CategoriaRepository>();
builder.Services.AddSingleton<UsuarioRepository>();
builder.Services.AddSingleton<LogUsoRepository>();
builder.Services.AddSingleton<MacroVersaoRepository>();
builder.Services.AddSingleton<LogAuditoriaRepository>();
builder.Services.AddSingleton<GrupoRepository>();
builder.Services.AddSingleton<FavoritoUsuarioRepository>();

// Services — IaService sem IHttpClientFactory
// MacroService/UsuarioService são Scoped (1 instância por requisição) porque UsuarioService.UsuarioAtual
// é preenchido por requisição a partir do JWT — Singleton causaria condição de corrida entre requisições.
builder.Services.AddScoped<MacroService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddSingleton<CategoriaService>();
builder.Services.AddSingleton<LogUsoService>();
builder.Services.AddSingleton<IaService>(sp =>
{
    var svc = new IaService();
    svc.ApiKey = builder.Configuration["Anthropic:ApiKey"] ?? string.Empty;
    return svc;
});

var app = builder.Build();

await app.Services.GetRequiredService<SupabaseContext>().InitializeAsync();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SK MacroHelper API v1"));
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Reflete o usuário do token JWT no UsuarioService (scoped) desta requisição.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var usuarioId = MacroHelper.API.Middleware.JwtHelper.ObterUsuarioId(context.User);
        if (usuarioId.HasValue)
        {
            var repo    = context.RequestServices.GetRequiredService<UsuarioRepository>();
            var usuario = await repo.GetByIdAsync(usuarioId.Value);
            if (usuario != null)
                context.RequestServices.GetRequiredService<UsuarioService>().DefinirUsuarioAtual(usuario);
        }
    }
    await next();
});

app.MapControllers();
app.MapHub<SyncHub>("/syncHub");
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", servidor = "SK MacroHelper API" }));
app.Run();
