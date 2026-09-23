using Microsoft.EntityFrameworkCore;

namespace AH.Api.Data;

// Contexto vacio: las entidades reales (usuarios, etc.) se agregan en la tarea 2,
// cuando /api/auth pasa a usar base de datos en vez de la lista en memoria.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
