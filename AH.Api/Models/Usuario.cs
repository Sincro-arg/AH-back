using System.ComponentModel.DataAnnotations;

namespace AH.Api.Models;

public class Usuario
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    [MaxLength(10)]
    public string Tema { get; set; } = "claro";

    public DateTime? UltimoAcceso { get; set; }

    public bool NotificacionesEmail { get; set; } = true;
}
