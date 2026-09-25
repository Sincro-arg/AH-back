using AH.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AH.Api.Controllers;

[ApiController]
[Route("api/pozos")]
public class PozosController : ControllerBase
{
    private readonly AppDbContext _context;

    public PozosController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Listado publico de pozos, sin autenticacion: es lo que se muestra en la
    /// landing para dar una senal de que el servidor esta vivo con datos reales.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var pozos = await _context.Pozos
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new
            {
                id = p.Id.ToString(),
                titulo = p.Titulo,
                autoDescripcion = p.AutoDescripcion,
                montoObjetivo = p.MontoObjetivo,
                montoRecaudado = p.MontoRecaudado,
                estado = p.Estado,
                fechaCreacion = p.FechaCreacion.ToString("o"),
                precioCompra = p.PrecioCompra,
                fechaCompra = p.FechaCompra.HasValue ? p.FechaCompra.Value.ToString("o") : null,
                precioVenta = p.PrecioVenta,
                fechaVenta = p.FechaVenta.HasValue ? p.FechaVenta.Value.ToString("o") : null,
            })
            .ToListAsync();

        return Ok(pozos);
    }
}
