using Microsoft.AspNetCore.Mvc;

namespace AH.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
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
}
