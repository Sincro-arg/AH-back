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

    // 21 pozos de ejemplo (13 Abierto, 4 Comprado, 4 Vendido), con estados
    // variados, para que la landing, el listado publico y el carrusel de
    // pozos recientes de la home tengan datos reales y variados desde el
    // primer arranque.
    // Devuelve el pozo 'VW Gol Trend 2019' recien creado (o null si los
    // pozos ya existian de una corrida anterior) para que SeedInversiones lo
    // reciba por parametro en vez de tener que volver a buscarlo por Titulo:
    // el Titulo es el nombre del auto y el modelo Pozo no lo restringe a ser
    // unico, asi que buscarlo con SingleOrDefault podia reventar si en el
    // futuro dos pozos comparten nombre.
    public static Pozo? SeedPozos(AppDbContext db)
    {
        if (db.Pozos.Any())
            return null;

        var pozoVwGol = new Pozo
        {
            Titulo = "VW Gol Trend 2019",
            AutoDescripcion = "VW Gol Trend 2019, 62.000 km, nafta, unico dueño.",
            MontoObjetivo = 2500000m,
            MontoRecaudado = 2500000m,
            Estado = "Comprado",
            PrecioCompra = 2450000m,
            FechaCompra = DateTime.UtcNow.AddDays(-10),
            FechaCreacion = DateTime.UtcNow.AddDays(-30),
            ImagenUrl = "https://cdn.example.com/pozos/vw-gol-trend-2019.jpg",
        };

        db.Pozos.AddRange(
            new Pozo
            {
                Titulo = "Fiat Cronos 2021",
                AutoDescripcion = "Fiat Cronos 2021, 45.000 km, nafta, full, unico dueño, ubicado en CABA.",
                MontoObjetivo = 3000000m,
                MontoRecaudado = 1200000m,
                PrecioVentaEstimado = 3800000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow,
                ImagenUrl = "https://cdn.example.com/pozos/fiat-cronos-2021.jpg",
            },
            pozoVwGol,
            new Pozo
            {
                Titulo = "Toyota Corolla 2018",
                AutoDescripcion = "Toyota Corolla 2018, 80.000 km, nafta, service oficial, ubicado en Rosario, Santa Fe.",
                MontoObjetivo = 4000000m,
                MontoRecaudado = 4000000m,
                Estado = "Vendido",
                PrecioCompra = 3900000m,
                FechaCompra = DateTime.UtcNow.AddDays(-60),
                PrecioVenta = 4600000m,
                FechaVenta = DateTime.UtcNow.AddDays(-5),
                FechaCreacion = DateTime.UtcNow.AddDays(-90),
                ImagenUrl = "https://cdn.example.com/pozos/toyota-corolla-2018.jpg",
            },
            new Pozo
            {
                Titulo = "Ford Focus 2016",
                AutoDescripcion = "Ford Focus 2016, motor 2.0 nafta, 95.000 km, muy cuidado, ubicado en Cordoba capital, listo para reventa.",
                MontoObjetivo = 7000000m,
                MontoRecaudado = 0m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-2),
                PrecioVentaEstimado = 8200000m,
                ImagenUrl = "https://cdn.example.com/pozos/ford-focus-2016.jpg",
            },
            new Pozo
            {
                Titulo = "Chevrolet Onix 2020",
                AutoDescripcion = "Chevrolet Onix 2020, 1.2 nafta, 38.000 km, unico dueño, ubicado en La Plata, Buenos Aires.",
                MontoObjetivo = 5500000m,
                MontoRecaudado = 2100000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-7),
                PrecioVentaEstimado = 6800000m,
                ImagenUrl = "https://cdn.example.com/pozos/chevrolet-onix-2020.jpg",
            },
            new Pozo
            {
                Titulo = "Renault Sandero Stepway 2017",
                AutoDescripcion = "Renault Sandero Stepway 2017, motor 1.6 nafta, 110.000 km, ubicado en Mendoza capital, service al dia.",
                MontoObjetivo = 4500000m,
                MontoRecaudado = 4500000m,
                Estado = "Comprado",
                FechaCreacion = DateTime.UtcNow.AddDays(-25),
                PrecioCompra = 4300000m,
                FechaCompra = DateTime.UtcNow.AddDays(-8),
                PrecioVentaEstimado = 5400000m,
                ImagenUrl = "https://cdn.example.com/pozos/renault-sandero-stepway-2017.jpg",
            },
            new Pozo
            {
                Titulo = "Peugeot 208 2019",
                AutoDescripcion = "Peugeot 208 2019, 1.6 nafta, 70.000 km, ubicado en Mar del Plata, Buenos Aires, service oficial completo.",
                MontoObjetivo = 6200000m,
                MontoRecaudado = 6200000m,
                Estado = "Vendido",
                FechaCreacion = DateTime.UtcNow.AddDays(-70),
                PrecioCompra = 5900000m,
                FechaCompra = DateTime.UtcNow.AddDays(-50),
                PrecioVenta = 7100000m,
                FechaVenta = DateTime.UtcNow.AddDays(-3),
                PrecioVentaEstimado = 7100000m,
                ImagenUrl = "https://cdn.example.com/pozos/peugeot-208-2019.jpg",
            },
            new Pozo
            {
                Titulo = "Honda Civic 2020",
                AutoDescripcion = "Honda Civic 2020, 1.5 turbo nafta, 52.000 km, full, unico dueño, ubicado en CABA.",
                MontoObjetivo = 8500000m,
                MontoRecaudado = 3400000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-4),
                PrecioVentaEstimado = 10200000m,
                ImagenUrl = "https://cdn.example.com/pozos/honda-civic-2020.jpg",
            },
            new Pozo
            {
                Titulo = "Volkswagen Vento 2018",
                AutoDescripcion = "Volkswagen Vento 2018, 2.0 tdi diesel, 90.000 km, service oficial, ubicado en Rosario, Santa Fe.",
                MontoObjetivo = 6800000m,
                MontoRecaudado = 1500000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-1),
                PrecioVentaEstimado = 8100000m,
                ImagenUrl = "https://cdn.example.com/pozos/vw-vento-2018.jpg",
            },
            new Pozo
            {
                Titulo = "Fiat Toro 2021",
                AutoDescripcion = "Fiat Toro 2021, 2.0 diesel 4x4, 40.000 km, unico dueño, ubicado en Cordoba capital.",
                MontoObjetivo = 9500000m,
                MontoRecaudado = 0m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-1),
                PrecioVentaEstimado = 11800000m,
                ImagenUrl = "https://cdn.example.com/pozos/fiat-toro-2021.jpg",
            },
            new Pozo
            {
                Titulo = "Renault Kangoo 2020",
                AutoDescripcion = "Renault Kangoo 2020, 1.6 nafta, 55.000 km, uso comercial, ubicado en Zarate, Buenos Aires.",
                MontoObjetivo = 4200000m,
                MontoRecaudado = 900000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-3),
                PrecioVentaEstimado = 5100000m,
                ImagenUrl = "https://cdn.example.com/pozos/renault-kangoo-2020.jpg",
            },
            new Pozo
            {
                Titulo = "Jeep Renegade 2019",
                AutoDescripcion = "Jeep Renegade 2019, 1.8 nafta 4x2, 68.000 km, unico dueño, ubicado en Neuquen capital.",
                MontoObjetivo = 7800000m,
                MontoRecaudado = 0m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow,
                PrecioVentaEstimado = 9300000m,
                ImagenUrl = "https://cdn.example.com/pozos/jeep-renegade-2019.jpg",
            },
            new Pozo
            {
                Titulo = "Nissan Kicks 2021",
                AutoDescripcion = "Nissan Kicks 2021, 1.6 nafta CVT, 30.000 km, full, unico dueño, ubicado en Salta capital.",
                MontoObjetivo = 8900000m,
                MontoRecaudado = 2500000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-6),
                PrecioVentaEstimado = 10600000m,
                ImagenUrl = "https://cdn.example.com/pozos/nissan-kicks-2021.jpg",
            },
            new Pozo
            {
                Titulo = "Ford Ka 2017",
                AutoDescripcion = "Ford Ka 2017, 1.5 nafta, 75.000 km, unico dueño, ubicado en Bahia Blanca, Buenos Aires.",
                MontoObjetivo = 3200000m,
                MontoRecaudado = 800000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-2),
                PrecioVentaEstimado = 3900000m,
                ImagenUrl = "https://cdn.example.com/pozos/ford-ka-2017.jpg",
            },
            new Pozo
            {
                Titulo = "Toyota Hilux 2019",
                AutoDescripcion = "Toyota Hilux 2019, 2.8 diesel 4x4, 95.000 km, doble cabina, ubicado en Tucuman capital.",
                MontoObjetivo = 12000000m,
                MontoRecaudado = 5000000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-9),
                PrecioVentaEstimado = 14500000m,
                ImagenUrl = "https://cdn.example.com/pozos/toyota-hilux-2019.jpg",
            },
            new Pozo
            {
                Titulo = "Chevrolet Tracker 2022",
                AutoDescripcion = "Chevrolet Tracker 2022, 1.2 turbo nafta, 15.000 km, unico dueño, ubicado en CABA.",
                MontoObjetivo = 9800000m,
                MontoRecaudado = 0m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow,
                PrecioVentaEstimado = 11700000m,
                ImagenUrl = "https://cdn.example.com/pozos/chevrolet-tracker-2022.jpg",
            },
            new Pozo
            {
                Titulo = "Citroen C4 Cactus 2020",
                AutoDescripcion = "Citroen C4 Cactus 2020, 1.6 nafta, 42.000 km, full, ubicado en Rosario, Santa Fe.",
                MontoObjetivo = 5900000m,
                MontoRecaudado = 3100000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-5),
                PrecioVentaEstimado = 7000000m,
                ImagenUrl = "https://cdn.example.com/pozos/citroen-c4-cactus-2020.jpg",
            },
            new Pozo
            {
                Titulo = "Peugeot 308 2015",
                AutoDescripcion = "Peugeot 308 2015, 1.6 nafta, 130.000 km, service oficial, ubicado en Cordoba capital.",
                MontoObjetivo = 3600000m,
                MontoRecaudado = 3600000m,
                Estado = "Comprado",
                FechaCreacion = DateTime.UtcNow.AddDays(-20),
                PrecioCompra = 3450000m,
                FechaCompra = DateTime.UtcNow.AddDays(-4),
                PrecioVentaEstimado = 4300000m,
                ImagenUrl = "https://cdn.example.com/pozos/peugeot-308-2015.jpg",
            },
            new Pozo
            {
                Titulo = "Volkswagen Suran 2016",
                AutoDescripcion = "Volkswagen Suran 2016, 1.6 nafta, 105.000 km, unico dueño, ubicado en La Plata, Buenos Aires.",
                MontoObjetivo = 3400000m,
                MontoRecaudado = 3400000m,
                Estado = "Comprado",
                FechaCreacion = DateTime.UtcNow.AddDays(-18),
                PrecioCompra = 3250000m,
                FechaCompra = DateTime.UtcNow.AddDays(-6),
                PrecioVentaEstimado = 4000000m,
                ImagenUrl = "https://cdn.example.com/pozos/vw-suran-2016.jpg",
            },
            new Pozo
            {
                Titulo = "Fiat Argo 2019",
                AutoDescripcion = "Fiat Argo 2019, 1.3 nafta, 60.000 km, unico dueño, ubicado en Mendoza capital.",
                MontoObjetivo = 4800000m,
                MontoRecaudado = 4800000m,
                Estado = "Vendido",
                FechaCreacion = DateTime.UtcNow.AddDays(-80),
                PrecioCompra = 4600000m,
                FechaCompra = DateTime.UtcNow.AddDays(-55),
                PrecioVenta = 5500000m,
                FechaVenta = DateTime.UtcNow.AddDays(-7),
                PrecioVentaEstimado = 5500000m,
                ImagenUrl = "https://cdn.example.com/pozos/fiat-argo-2019.jpg",
            },
            new Pozo
            {
                Titulo = "Renault Duster 2018",
                AutoDescripcion = "Renault Duster 2018, 1.6 nafta 4x2, 88.000 km, ubicado en Neuquen capital, service al dia.",
                MontoObjetivo = 5300000m,
                MontoRecaudado = 5300000m,
                Estado = "Vendido",
                FechaCreacion = DateTime.UtcNow.AddDays(-65),
                PrecioCompra = 5100000m,
                FechaCompra = DateTime.UtcNow.AddDays(-40),
                PrecioVenta = 6200000m,
                FechaVenta = DateTime.UtcNow.AddDays(-2),
                PrecioVentaEstimado = 6200000m,
                ImagenUrl = "https://cdn.example.com/pozos/renault-duster-2018.jpg",
            }
        );
        db.SaveChanges();
        return pozoVwGol;
    }

    // Varias inversiones de ejemplo del admin, en pozos con estados distintos
    // (Abierto/Comprado/Vendido), para que la pantalla "Mis inversiones" se
    // vea con varias tarjetas al entrar con las credenciales de demo en vez
    // de una sola. La primera (VW Gol Trend 2019) recibe el pozo por
    // parametro -el que devolvio SeedPozos- en vez de buscarlo por Titulo,
    // para no depender de que el Titulo sea unico; las demas se buscan por
    // Titulo con FirstOrDefault (no SingleOrDefault) por la misma razon.
    // Cada Monto es la mitad del MontoRecaudado ya sembrado en ese pozo (no
    // lo modifica ni lo supera) y cada Fecha es posterior a la FechaCreacion
    // del pozo.
    public static void SeedInversiones(AppDbContext db, Pozo? pozoVwGol)
    {
        if (db.Inversiones.Any())
            return;

        var admin = db.Usuarios.SingleOrDefault(u => u.Email == AdminEmail);
        if (admin is null)
            return;

        var inversiones = new List<Inversion>();

        if (pozoVwGol is not null)
        {
            inversiones.Add(new Inversion
            {
                PozoId = pozoVwGol.Id,
                UsuarioId = admin.Id,
                Monto = pozoVwGol.MontoRecaudado,
                Fecha = pozoVwGol.FechaCompra ?? pozoVwGol.FechaCreacion,
            });
        }

        void AgregarSiExiste(string titulo, int diasDespuesDeCreado)
        {
            var pozo = db.Pozos.FirstOrDefault(p => p.Titulo == titulo);
            if (pozo is null)
                return;

            inversiones.Add(new Inversion
            {
                PozoId = pozo.Id,
                UsuarioId = admin.Id,
                Monto = pozo.MontoRecaudado / 2,
                Fecha = pozo.FechaCreacion.AddDays(diasDespuesDeCreado),
            });
        }

        AgregarSiExiste("Fiat Cronos 2021", 0);
        AgregarSiExiste("Chevrolet Onix 2020", 1);
        AgregarSiExiste("Renault Sandero Stepway 2017", 2);
        AgregarSiExiste("Peugeot 208 2019", 3);
        AgregarSiExiste("Honda Civic 2020", 0);

        db.Inversiones.AddRange(inversiones);
        db.SaveChanges();
    }
}
