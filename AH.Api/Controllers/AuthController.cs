using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AH.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public record RegisterDto(string Nombre, string Apellido, string Email, string Telefono, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto == null)
            return BadRequest(new { error = "Datos inválidos" });

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { error = "El nombre es requerido" });

        if (string.IsNullOrWhiteSpace(dto.Apellido))
            return BadRequest(new { error = "El apellido es requerido" });

        if (!EsEmailValido(dto.Email))
            return BadRequest(new { error = "El email no tiene un formato válido" });

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            return BadRequest(new { error = "La contraseña debe tener al menos 8 caracteres" });

        var emailNorm = dto.Email.Trim().ToLower();

        if (await _context.Usuarios.AnyAsync(u => u.Email == emailNorm))
            return Conflict(new { error = "El email ya está registrado" });

        var usuario = new Usuario
        {
            Nombre       = dto.Nombre.Trim(),
            Apellido     = dto.Apellido.Trim(),
            Email        = emailNorm,
            Telefono     = dto.Telefono?.Trim() ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FechaAlta    = DateTime.UtcNow,
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Usuario registrado correctamente" });
    }

    public record LoginDto(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Unauthorized(new { error = "Email o contraseña incorrectos" });

        var emailNorm = dto.Email.Trim().ToLower();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNorm);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            return Unauthorized(new { error = "Email o contraseña incorrectos" });

        usuario.UltimoAcceso = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateToken(usuario);

        return Ok(new
        {
            token,
            usuario = new
            {
                id = usuario.Id.ToString(),
                nombre = usuario.Nombre,
                apellido = usuario.Apellido,
                email = usuario.Email,
                telefono = usuario.Telefono,
                tema = usuario.Tema,
                fechaAlta = usuario.FechaAlta.ToString("o"),
                ultimoAcceso = usuario.UltimoAcceso.HasValue ? usuario.UltimoAcceso.Value.ToString("o") : null,
                notificacionesEmail = usuario.NotificacionesEmail,
            },
        });
    }

    private string GenerateToken(Usuario usuario)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nombre", usuario.Nombre),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var expireHours = int.TryParse(_config["Jwt:ExpireHours"], out var h) ? h : 8;

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expireHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool EsEmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email.Trim());
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
}
