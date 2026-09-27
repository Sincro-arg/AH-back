using System.ComponentModel.DataAnnotations;

namespace AH.Api.Models;

public class Inversion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PozoId { get; set; }

    public Pozo? Pozo { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public decimal Monto { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
