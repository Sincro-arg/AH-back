using System.ComponentModel.DataAnnotations;

namespace AH.Api.Models;

public class Pozo
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public string AutoDescripcion { get; set; } = string.Empty;

    public decimal MontoObjetivo { get; set; }

    public decimal MontoRecaudado { get; set; }

    [Required, MaxLength(20)]
    public string Estado { get; set; } = "Abierto";

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public decimal? PrecioCompra { get; set; }

    public DateTime? FechaCompra { get; set; }

    public decimal? PrecioVenta { get; set; }

    public DateTime? FechaVenta { get; set; }
}
