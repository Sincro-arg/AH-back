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

    // Publico: sin [Authorize]. La landing lo usa para mostrar el listado
    // de pozos sin que el visitante tenga que loguearse.
    [HttpGet]
    public async Task<IActionResult> GetPozos()
    {
        var pozos = await _context.Pozos
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();

        var resultado = pozos.Select(p => new
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
            imagenUrl = p.ImagenUrl,
            precioVentaEstimado = p.PrecioVentaEstimado,
        });

        return Ok(resultado);
    }
}
