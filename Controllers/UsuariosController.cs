using AH.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        });
    }
}
