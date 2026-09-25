using AH.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AH.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized(new { error = "Token inválido" });

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return Unauthorized(new { error = "Token inválido" });

        return Ok(new
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
        });
    }

    public record UpdateMeDto(string Nombre, string Apellido, string Telefono, string Email);

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized(new { error = "Token inválido" });

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return Unauthorized(new { error = "Token inválido" });

        if (dto == null)
            return BadRequest(new { error = "Datos inválidos" });

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { error = "El nombre es requerido" });

        if (string.IsNullOrWhiteSpace(dto.Apellido))
            return BadRequest(new { error = "El apellido es requerido" });

        if (!EsEmailValido(dto.Email))
            return BadRequest(new { error = "El email no tiene un formato válido" });

        var emailNorm = dto.Email.Trim().ToLower();

        if (await _context.Usuarios.AnyAsync(u => u.Email == emailNorm && u.Id != id))
            return Conflict(new { error = "El email ya esta registrado" });

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Apellido = dto.Apellido.Trim();
        usuario.Telefono = dto.Telefono?.Trim() ?? string.Empty;
        usuario.Email = emailNorm;

        await _context.SaveChangesAsync();

        return Ok(new
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
        });
    }

    public record UpdatePasswordDto(string PasswordActual, string PasswordNueva);

    [HttpPut("me/password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized(new { error = "Token inválido" });

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return Unauthorized(new { error = "Token inválido" });

        if (dto == null || !BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
            return BadRequest(new { error = "La contraseña actual no es correcta" });

        if (string.IsNullOrWhiteSpace(dto.PasswordNueva) || dto.PasswordNueva.Length < 8)
            return BadRequest(new { error = "La nueva contraseña debe tener al menos 8 caracteres" });

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Contraseña actualizada" });
    }

    public record UpdateTemaDto(string Tema);

    [HttpPut("me/tema")]
    public async Task<IActionResult> UpdateTema([FromBody] UpdateTemaDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized(new { error = "Token inválido" });

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return Unauthorized(new { error = "Token inválido" });

        if (dto == null || (dto.Tema != "claro" && dto.Tema != "oscuro"))
            return BadRequest(new { error = "El tema debe ser 'claro' u 'oscuro'" });

        usuario.Tema = dto.Tema;
        await _context.SaveChangesAsync();

        return Ok(new { tema = usuario.Tema });
    }

    public record UpdateNotificacionesDto(bool NotificacionesEmail);

    [HttpPut("me/notificaciones")]
    public async Task<IActionResult> UpdateNotificaciones([FromBody] UpdateNotificacionesDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized(new { error = "Token inválido" });

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return Unauthorized(new { error = "Token inválido" });

        if (dto == null)
            return BadRequest(new { error = "Datos inválidos" });

        usuario.NotificacionesEmail = dto.NotificacionesEmail;
        await _context.SaveChangesAsync();

        return Ok(new { notificacionesEmail = usuario.NotificacionesEmail });
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized(new { error = "Token inválido" });

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return Unauthorized(new { error = "Token inválido" });

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        return NoContent();
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
