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
}
