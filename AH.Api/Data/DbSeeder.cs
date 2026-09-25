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

    // Tres pozos de ejemplo, uno por cada estado, para que la landing y el
    // listado publico tengan datos reales desde el primer arranque.
    public static void SeedPozos(AppDbContext db)
    {
        if (db.Pozos.Any())
            return;

        db.Pozos.AddRange(
            new Pozo
            {
                Titulo = "Fiat Cronos 2021",
                AutoDescripcion = "Fiat Cronos 2021, 45.000 km, nafta, full.",
                MontoObjetivo = 3000000m,
                MontoRecaudado = 1200000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow,
            },
            new Pozo
            {
                Titulo = "VW Gol Trend 2019",
                AutoDescripcion = "VW Gol Trend 2019, 62.000 km, nafta, unico dueño.",
                MontoObjetivo = 2500000m,
                MontoRecaudado = 2500000m,
                Estado = "Comprado",
                PrecioCompra = 2450000m,
                FechaCompra = DateTime.UtcNow.AddDays(-10),
                FechaCreacion = DateTime.UtcNow.AddDays(-30),
            },
            new Pozo
            {
                Titulo = "Toyota Corolla 2018",
                AutoDescripcion = "Toyota Corolla 2018, 80.000 km, nafta, service oficial.",
                MontoObjetivo = 4000000m,
                MontoRecaudado = 4000000m,
                Estado = "Vendido",
                PrecioCompra = 3900000m,
                FechaCompra = DateTime.UtcNow.AddDays(-60),
                PrecioVenta = 4600000m,
                FechaVenta = DateTime.UtcNow.AddDays(-5),
                FechaCreacion = DateTime.UtcNow.AddDays(-90),
            }
        );
        db.SaveChanges();
    }
}
