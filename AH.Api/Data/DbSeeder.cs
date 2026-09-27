using AH.Api.Models;

namespace AH.Api.Data;

// Datos de ejemplo pedidos por el pliego: un usuario de prueba ya dado de
// alta para poder loguearse sin tener que registrarse antes.
public static class DbSeeder
{
    public const string AdminEmail = "admin@cuentas.com";
    public const string AdminPassword = "Admin123!";

    public static void SeedAdminUsuario(AppDbContext db)
    {
        if (db.Usuarios.Any(u => u.Email == AdminEmail))
            return;

        db.Usuarios.Add(new Usuario
        {
            Nombre = "Admin",
            Apellido = "Cuentas",
            Email = AdminEmail,
            Telefono = "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
            FechaAlta = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    // Tres pozos de ejemplo, uno por cada estado, para que la demo (y la
    // landing publica) tengan datos desde el primer arranque.
    public static void SeedPozos(AppDbContext db)
    {
        if (db.Pozos.Any())
            return;

        db.Pozos.AddRange(
            new Pozo
            {
                Titulo = "Toyota Corolla 2015",
                AutoDescripcion = "Toyota Corolla 2015, motor 1.8, muy buen estado general.",
                MontoObjetivo = 8000000m,
                MontoRecaudado = 0m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-3),
                ImagenUrl = "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
                PrecioVentaEstimado = 9200000m,
            },
            new Pozo
            {
                Titulo = "Volkswagen Gol Trend 2018",
                AutoDescripcion = "VW Gol Trend 2018, unico dueño, service oficial al dia.",
                MontoObjetivo = 6000000m,
                MontoRecaudado = 6000000m,
                Estado = "Comprado",
                FechaCreacion = DateTime.UtcNow.AddDays(-20),
                PrecioCompra = 5800000m,
                FechaCompra = DateTime.UtcNow.AddDays(-10),
                ImagenUrl = "https://images.unsplash.com/photo-1541443131876-44b03de101c5?w=800&q=80",
                PrecioVentaEstimado = 7000000m,
            },
            new Pozo
            {
                Titulo = "Ford Focus 2016",
                AutoDescripcion = "Ford Focus 2016, nafta, muy cuidado, listo para reventa.",
                MontoObjetivo = 7000000m,
                MontoRecaudado = 7000000m,
                Estado = "Vendido",
                FechaCreacion = DateTime.UtcNow.AddDays(-45),
                PrecioCompra = 6700000m,
                FechaCompra = DateTime.UtcNow.AddDays(-35),
                PrecioVenta = 8200000m,
                FechaVenta = DateTime.UtcNow.AddDays(-5),
                ImagenUrl = "https://images.unsplash.com/photo-1494905998402-395d579af36f?w=800&q=80",
                PrecioVentaEstimado = 8200000m,
            }
        );
        db.SaveChanges();
    }
}
