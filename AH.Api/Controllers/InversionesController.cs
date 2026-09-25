using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AH.Api.Controllers;

[ApiController]
public class InversionesController : ControllerBase
{
    private readonly AppDbContext _context;

    public InversionesController(AppDbContext context)
    {
        _context = context;
    }

    public record CrearInversionDto(decimal Monto);

    /// <summary>
    /// Carga una inversion en un pozo Abierto y recalcula el monto recaudado del pozo.
    /// </summary>
    [HttpPost("api/pozos/{id}/inversiones")]
    [Authorize]
    public async Task<IActionResult> Crear(Guid id, [FromBody] CrearInversionDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var usuarioId))
            return Unauthorized(new { error = "Token inválido" });

        var pozo = await _context.Pozos.FindAsync(id);
        if (pozo == null)
            return NotFound(new { error = "El pozo no existe" });

        if (dto == null || dto.Monto <= 0)
            return BadRequest(new { error = "El monto debe ser mayor a cero" });

        if (pozo.Estado != "Abierto")
            return Conflict(new { error = "El pozo no está abierto para recibir inversiones" });

        var inversion = new Inversion
        {
            PozoId = id,
            UsuarioId = usuarioId,
            Monto = dto.Monto,
        };
        _context.Inversiones.Add(inversion);

        pozo.MontoRecaudado += dto.Monto;

        await _context.SaveChangesAsync();

        return StatusCode(201, MapInversion(inversion));
    }

    /// <summary>
    /// Listado de inversiones de un pozo, con el nombre del inversor.
    /// </summary>
    [HttpGet("api/pozos/{id}/inversiones")]
    [Authorize]
    public async Task<IActionResult> Listar(Guid id)
    {
        var pozoExiste = await _context.Pozos.AnyAsync(p => p.Id == id);
        if (!pozoExiste)
            return NotFound(new { error = "El pozo no existe" });

        var inversiones = await _context.Inversiones
            .Where(i => i.PozoId == id)
            .OrderBy(i => i.Fecha)
            .Join(_context.Usuarios, i => i.UsuarioId, u => u.Id, (i, u) => new
            {
                id = i.Id.ToString(),
                usuarioId = i.UsuarioId.ToString(),
                nombreInversor = u.Nombre + " " + u.Apellido,
                monto = i.Monto,
                fecha = i.Fecha.ToString("o"),
            })
            .ToListAsync();

        return Ok(inversiones);
    }

    public record EditarInversionDto(decimal Monto);

    /// <summary>
    /// Edita el monto de una inversion propia y recalcula el monto recaudado del pozo.
    /// </summary>
    [HttpPut("api/inversiones/{id}")]
    [Authorize]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarInversionDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var usuarioId))
            return Unauthorized(new { error = "Token inválido" });

        var inversion = await _context.Inversiones.FindAsync(id);
        if (inversion == null)
            return NotFound(new { error = "La inversión no existe" });

        if (inversion.UsuarioId != usuarioId)
            return StatusCode(403, new { error = "No sos el dueño de esta inversión" });

        if (dto == null || dto.Monto <= 0)
            return BadRequest(new { error = "El monto debe ser mayor a cero" });

        var pozo = await _context.Pozos.FindAsync(inversion.PozoId);
        if (pozo == null)
            return NotFound(new { error = "El pozo no existe" });

        if (pozo.Estado != "Abierto")
            return Conflict(new { error = "El pozo no está abierto" });

        pozo.MontoRecaudado += dto.Monto - inversion.Monto;
        inversion.Monto = dto.Monto;

        await _context.SaveChangesAsync();

        return Ok(MapInversion(inversion));
    }

    /// <summary>
    /// Borra una inversion propia y recalcula el monto recaudado del pozo.
    /// </summary>
    [HttpDelete("api/inversiones/{id}")]
    [Authorize]
    public async Task<IActionResult> Borrar(Guid id)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (idClaim == null || !Guid.TryParse(idClaim, out var usuarioId))
            return Unauthorized(new { error = "Token inválido" });

        var inversion = await _context.Inversiones.FindAsync(id);
        if (inversion == null)
            return NotFound(new { error = "La inversión no existe" });

        if (inversion.UsuarioId != usuarioId)
            return StatusCode(403, new { error = "No sos el dueño de esta inversión" });

        var pozo = await _context.Pozos.FindAsync(inversion.PozoId);
        if (pozo == null)
            return NotFound(new { error = "El pozo no existe" });

        if (pozo.Estado != "Abierto")
            return Conflict(new { error = "El pozo no está abierto" });

        pozo.MontoRecaudado -= inversion.Monto;
        _context.Inversiones.Remove(inversion);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static object MapInversion(Inversion inversion) => new
    {
        id = inversion.Id.ToString(),
        pozoId = inversion.PozoId.ToString(),
        usuarioId = inversion.UsuarioId.ToString(),
        monto = inversion.Monto,
        fecha = inversion.Fecha.ToString("o"),
    };
}
