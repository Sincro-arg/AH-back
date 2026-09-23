using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AH.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    public record DemoUsuarioDto(string Id, string Nombre, string Apellido, string Email);

    private static readonly List<DemoUsuarioDto> DemoUsuarios =
    [
        new("1", "Ana", "Gomez", "ana.gomez@example.com"),
        new("2", "Luis", "Perez", "luis.perez@example.com"),
    ];

    // Ruta temporal del esqueleto (tarea 1), sin auth ni base de datos.
    // Se reemplaza en la tarea 2 por datos reales.
    [HttpGet("demo-usuarios")]
    public ActionResult<IEnumerable<DemoUsuarioDto>> GetDemoUsuarios() => Ok(DemoUsuarios);

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
