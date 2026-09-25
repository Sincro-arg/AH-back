using AH.Api.Data;
using Microsoft.AspNetCore.Authorization;
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

    /// <summary>
    /// Detalle de un pozo con sus inversiones, para el inversor autenticado.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var pozo = await _context.Pozos.FindAsync(id);
        if (pozo == null)
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

        return Ok(new
        {
            id = pozo.Id.ToString(),
            titulo = pozo.Titulo,
            autoDescripcion = pozo.AutoDescripcion,
            montoObjetivo = pozo.MontoObjetivo,
            montoRecaudado = pozo.MontoRecaudado,
            estado = pozo.Estado,
            fechaCreacion = pozo.FechaCreacion.ToString("o"),
            precioCompra = pozo.PrecioCompra,
            fechaCompra = pozo.FechaCompra.HasValue ? pozo.FechaCompra.Value.ToString("o") : null,
            precioVenta = pozo.PrecioVenta,
            fechaVenta = pozo.FechaVenta.HasValue ? pozo.FechaVenta.Value.ToString("o") : null,
            inversiones,
        });
    }
}
