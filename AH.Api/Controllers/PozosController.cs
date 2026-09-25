using AH.Api.Data;
using AH.Api.Models;
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

    public record CrearPozoDto(string Titulo, string AutoDescripcion, decimal MontoObjetivo);

    /// <summary>
    /// Alta de un pozo nuevo, arranca siempre en estado Abierto y sin nada recaudado.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Crear([FromBody] CrearPozoDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Titulo))
            return BadRequest(new { error = "El título es requerido" });

        if (dto.MontoObjetivo <= 0)
            return BadRequest(new { error = "El monto objetivo debe ser mayor a cero" });

        var pozo = new Pozo
        {
            Titulo = dto.Titulo.Trim(),
            AutoDescripcion = dto.AutoDescripcion?.Trim() ?? string.Empty,
            MontoObjetivo = dto.MontoObjetivo,
            MontoRecaudado = 0,
            Estado = "Abierto",
        };

        _context.Pozos.Add(pozo);
        await _context.SaveChangesAsync();

        return StatusCode(201, MapPozo(pozo));
    }

    public record ActualizarPozoDto(string Titulo, string AutoDescripcion, decimal MontoObjetivo);

    /// <summary>
    /// Edicion de titulo/descripcion/objetivo. Solo se puede mientras el pozo sigue Abierto.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarPozoDto dto)
    {
        var pozo = await _context.Pozos.FindAsync(id);
        if (pozo == null)
            return NotFound(new { error = "El pozo no existe" });

        if (dto == null || string.IsNullOrWhiteSpace(dto.Titulo))
            return BadRequest(new { error = "El título es requerido" });

        if (dto.MontoObjetivo <= 0)
            return BadRequest(new { error = "El monto objetivo debe ser mayor a cero" });

        if (pozo.Estado != "Abierto")
            return Conflict(new { error = "Solo se puede editar un pozo mientras está Abierto" });

        pozo.Titulo = dto.Titulo.Trim();
        pozo.AutoDescripcion = dto.AutoDescripcion?.Trim() ?? string.Empty;
        pozo.MontoObjetivo = dto.MontoObjetivo;

        await _context.SaveChangesAsync();

        return Ok(MapPozo(pozo));
    }

    /// <summary>
    /// Baja de un pozo. No se puede si ya tiene inversiones cargadas.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var pozo = await _context.Pozos.FindAsync(id);
        if (pozo == null)
            return NotFound(new { error = "El pozo no existe" });

        var tieneInversiones = await _context.Inversiones.AnyAsync(i => i.PozoId == id);
        if (tieneInversiones)
            return Conflict(new { error = "No se puede eliminar un pozo que ya tiene inversiones" });

        _context.Pozos.Remove(pozo);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    public record CambiarEstadoDto(string Accion, decimal? PrecioCompra, DateTime? FechaCompra, decimal? PrecioVenta, DateTime? FechaVenta);

    /// <summary>
    /// Transiciona el estado del pozo: Abierto -> Comprado -> Vendido.
    /// </summary>
    [HttpPut("{id}/estado")]
    [Authorize]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoDto dto)
    {
        var pozo = await _context.Pozos.FindAsync(id);
        if (pozo == null)
            return NotFound(new { error = "El pozo no existe" });

        if (dto == null || string.IsNullOrWhiteSpace(dto.Accion))
            return BadRequest(new { error = "La acción es requerida" });

        switch (dto.Accion)
        {
            case "marcarComprado":
                if (dto.PrecioCompra == null || dto.FechaCompra == null)
                    return BadRequest(new { error = "precioCompra y fechaCompra son requeridos" });

                if (pozo.Estado != "Abierto")
                    return Conflict(new { error = "Solo se puede marcar comprado un pozo Abierto" });

                pozo.PrecioCompra = dto.PrecioCompra;
                pozo.FechaCompra = dto.FechaCompra;
                pozo.Estado = "Comprado";
                break;

            case "marcarVendido":
                if (dto.PrecioVenta == null || dto.FechaVenta == null)
                    return BadRequest(new { error = "precioVenta y fechaVenta son requeridos" });

                if (pozo.Estado != "Comprado")
                    return Conflict(new { error = "Solo se puede marcar vendido un pozo Comprado" });

                pozo.PrecioVenta = dto.PrecioVenta;
                pozo.FechaVenta = dto.FechaVenta;
                pozo.Estado = "Vendido";
                break;

            default:
                return BadRequest(new { error = "Acción inválida" });
        }

        await _context.SaveChangesAsync();

        return Ok(MapPozo(pozo));
    }

    /// <summary>
    /// Reparto de la ganancia de un pozo Vendido, proporcional al monto de cada inversion.
    /// </summary>
    [HttpGet("{id}/reparto")]
    [Authorize]
    public async Task<IActionResult> Reparto(Guid id)
    {
        var pozo = await _context.Pozos.FindAsync(id);
        if (pozo == null)
            return NotFound(new { error = "El pozo no existe" });

        if (pozo.Estado != "Vendido")
            return Conflict(new { error = "El pozo todavía no fue vendido" });

        var gananciaTotal = pozo.PrecioVenta!.Value - pozo.PrecioCompra!.Value;

        var inversiones = await _context.Inversiones
            .Where(i => i.PozoId == id)
            .OrderBy(i => i.Fecha)
            .Join(_context.Usuarios, i => i.UsuarioId, u => u.Id, (i, u) => new
            {
                i.UsuarioId,
                NombreInversor = u.Nombre + " " + u.Apellido,
                i.Monto,
            })
            .ToListAsync();

        var reparto = inversiones.Select(i =>
        {
            var porcentaje = pozo.MontoRecaudado > 0 ? i.Monto / pozo.MontoRecaudado : 0m;
            return new
            {
                usuarioId = i.UsuarioId.ToString(),
                nombreInversor = i.NombreInversor,
                montoInvertido = i.Monto,
                porcentaje,
                ganancia = gananciaTotal * porcentaje,
            };
        }).ToList();

        return Ok(new
        {
            gananciaTotal,
            reparto,
        });
    }

    private static object MapPozo(Pozo pozo) => new
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
    };
}
