using AH.Api.Models;

namespace AH.Api.Data;

// Datos de ejemplo pedidos por el pliego: un usuario de prueba ya dado de
// alta para poder loguearse sin tener que registrarse antes.
public static class DbSeeder
{
    public const string AdminEmail = "admin@cuentas.com";
    public const string AdminPassword = "Admin123!";

    // Placeholder de imagen para todos los pozos de ejemplo: la misma foto
    // de auto que ya usa la home (AH-front/src/app/components/home/home.html)
    // y que tarjeta-pozo usa como fallback cuando no hay ImagenUrl o la URL
    // cargada esta rota. Se sirve como ruta relativa del propio front
    // (AH-front/public/imagenes), asi que resuelve bien en cualquier entorno
    // sin depender de un dominio externo como picsum.photos, que devolvia
    // fotos sin relacion con autos.
    private const string ImagenPlaceholder = "/imagenes/auto.jpg";

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

    // 31 pozos de ejemplo (17 Abierto, 7 Comprado, 7 Vendido), con estados
    // variados, para que la landing, el listado publico y el carrusel de
    // pozos recientes de la home tengan datos reales y variados desde el
    // primer arranque.
    // La siembra es ADITIVA por Titulo: cada pozo de la lista se busca con
    // FirstOrDefault (no SingleOrDefault, porque el modelo no exige Titulo
    // unico) y solo se agrega si todavia no existe, para poder correrla
    // sobre una base que ya tiene algunos de estos pozos (de una corrida
    // anterior con una lista mas corta, o cargados a mano por un usuario)
    // sin duplicar nada ni tocar lo que ya esta.
    // Devuelve el pozo 'VW Gol Trend 2019' (el ya existente si lo habia, o
    // el recien creado) para que SeedInversiones lo reciba por parametro en
    // vez de tener que volver a buscarlo por Titulo.
    public static Pozo? SeedPozos(AppDbContext db)
    {
        var nuevos = new List<Pozo>();

        Pozo AgregarSiFalta(Pozo pozo)
        {
            var existente = db.Pozos.FirstOrDefault(p => p.Titulo == pozo.Titulo);
            if (existente is not null)
                return existente;

            nuevos.Add(pozo);
            return pozo;
        }

        var pozoVwGol = AgregarSiFalta(new Pozo
        {
            Titulo = "VW Gol Trend 2019",
            AutoDescripcion = "VW Gol Trend 2019, 62.000 km, nafta, unico dueño.",
            MontoObjetivo = 2500000m,
            MontoRecaudado = 2500000m,
            Estado = "Comprado",
            PrecioCompra = 2450000m,
            FechaCompra = DateTime.UtcNow.AddDays(-10),
            FechaCreacion = DateTime.UtcNow.AddDays(-30),
            ImagenUrl = ImagenPlaceholder,
        });

        AgregarSiFalta(new Pozo
        {
            Titulo = "Fiat Cronos 2021",
            AutoDescripcion = "Fiat Cronos 2021, 45.000 km, nafta, full, unico dueño, ubicado en CABA.",
            MontoObjetivo = 3000000m,
            MontoRecaudado = 1200000m,
            PrecioVentaEstimado = 3800000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
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
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Ford Focus 2016",
            AutoDescripcion = "Ford Focus 2016, motor 2.0 nafta, 95.000 km, muy cuidado, ubicado en Cordoba capital, listo para reventa.",
            MontoObjetivo = 7000000m,
            MontoRecaudado = 0m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-2),
            PrecioVentaEstimado = 8200000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Chevrolet Onix 2020",
            AutoDescripcion = "Chevrolet Onix 2020, 1.2 nafta, 38.000 km, unico dueño, ubicado en La Plata, Buenos Aires.",
            MontoObjetivo = 5500000m,
            MontoRecaudado = 2100000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-7),
            PrecioVentaEstimado = 6800000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
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
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
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
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Honda Civic 2020",
            AutoDescripcion = "Honda Civic 2020, 1.5 turbo nafta, 52.000 km, full, unico dueño, ubicado en CABA.",
            MontoObjetivo = 8500000m,
            MontoRecaudado = 3400000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-4),
            PrecioVentaEstimado = 10200000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Volkswagen Vento 2018",
            AutoDescripcion = "Volkswagen Vento 2018, 2.0 tdi diesel, 90.000 km, service oficial, ubicado en Rosario, Santa Fe.",
            MontoObjetivo = 6800000m,
            MontoRecaudado = 1500000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-1),
            PrecioVentaEstimado = 8100000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Fiat Toro 2021",
            AutoDescripcion = "Fiat Toro 2021, 2.0 diesel 4x4, 40.000 km, unico dueño, ubicado en Cordoba capital.",
            MontoObjetivo = 9500000m,
            MontoRecaudado = 0m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-1),
            PrecioVentaEstimado = 11800000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Renault Kangoo 2020",
            AutoDescripcion = "Renault Kangoo 2020, 1.6 nafta, 55.000 km, uso comercial, ubicado en Zarate, Buenos Aires.",
            MontoObjetivo = 4200000m,
            MontoRecaudado = 900000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-3),
            PrecioVentaEstimado = 5100000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Jeep Renegade 2019",
            AutoDescripcion = "Jeep Renegade 2019, 1.8 nafta 4x2, 68.000 km, unico dueño, ubicado en Neuquen capital.",
            MontoObjetivo = 7800000m,
            MontoRecaudado = 0m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow,
            PrecioVentaEstimado = 9300000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Nissan Kicks 2021",
            AutoDescripcion = "Nissan Kicks 2021, 1.6 nafta CVT, 30.000 km, full, unico dueño, ubicado en Salta capital.",
            MontoObjetivo = 8900000m,
            MontoRecaudado = 2500000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-6),
            PrecioVentaEstimado = 10600000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Ford Ka 2017",
            AutoDescripcion = "Ford Ka 2017, 1.5 nafta, 75.000 km, unico dueño, ubicado en Bahia Blanca, Buenos Aires.",
            MontoObjetivo = 3200000m,
            MontoRecaudado = 800000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-2),
            PrecioVentaEstimado = 3900000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Toyota Hilux 2019",
            AutoDescripcion = "Toyota Hilux 2019, 2.8 diesel 4x4, 95.000 km, doble cabina, ubicado en Tucuman capital.",
            MontoObjetivo = 12000000m,
            MontoRecaudado = 5000000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-9),
            PrecioVentaEstimado = 14500000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Chevrolet Tracker 2022",
            AutoDescripcion = "Chevrolet Tracker 2022, 1.2 turbo nafta, 15.000 km, unico dueño, ubicado en CABA.",
            MontoObjetivo = 9800000m,
            MontoRecaudado = 0m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow,
            PrecioVentaEstimado = 11700000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Citroen C4 Cactus 2020",
            AutoDescripcion = "Citroen C4 Cactus 2020, 1.6 nafta, 42.000 km, full, ubicado en Rosario, Santa Fe.",
            MontoObjetivo = 5900000m,
            MontoRecaudado = 3100000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-5),
            PrecioVentaEstimado = 7000000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
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
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
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
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
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
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
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
            ImagenUrl = ImagenPlaceholder,
        });

        // 10 pozos nuevos que se suman a los 21 anteriores (4 Abierto, 3
        // Comprado, 3 Vendido) para que produccion, que quedo congelada en
        // la lista corta original, pase a tener variedad real al aplicar
        // esta siembra aditiva.
        AgregarSiFalta(new Pozo
        {
            Titulo = "Chevrolet Cruze 2017",
            AutoDescripcion = "Chevrolet Cruze 2017, 1.4 turbo nafta, 85.000 km, full, ubicado en Rosario, Santa Fe.",
            MontoObjetivo = 5200000m,
            MontoRecaudado = 1800000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-3),
            PrecioVentaEstimado = 6300000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Ford EcoSport 2018",
            AutoDescripcion = "Ford EcoSport 2018, 1.5 nafta, 72.000 km, unico dueño, ubicado en Cordoba capital.",
            MontoObjetivo = 4700000m,
            MontoRecaudado = 0m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-1),
            PrecioVentaEstimado = 5600000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Toyota Etios 2020",
            AutoDescripcion = "Toyota Etios 2020, 1.5 nafta, 35.000 km, unico dueño, ubicado en Mar del Plata, Buenos Aires.",
            MontoObjetivo = 3900000m,
            MontoRecaudado = 1600000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow.AddDays(-4),
            PrecioVentaEstimado = 4700000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Volkswagen Polo 2022",
            AutoDescripcion = "Volkswagen Polo 2022, 1.6 nafta, 12.000 km, unico dueño, ubicado en CABA.",
            MontoObjetivo = 6900000m,
            MontoRecaudado = 2400000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow,
            PrecioVentaEstimado = 8300000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Peugeot 3008 2019",
            AutoDescripcion = "Peugeot 3008 2019, 1.6 turbo nafta, 58.000 km, full, ubicado en La Plata, Buenos Aires.",
            MontoObjetivo = 8100000m,
            MontoRecaudado = 8100000m,
            Estado = "Comprado",
            FechaCreacion = DateTime.UtcNow.AddDays(-22),
            PrecioCompra = 7800000m,
            FechaCompra = DateTime.UtcNow.AddDays(-9),
            PrecioVentaEstimado = 9500000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Nissan Versa 2021",
            AutoDescripcion = "Nissan Versa 2021, 1.6 nafta CVT, 28.000 km, unico dueño, ubicado en Salta capital.",
            MontoObjetivo = 4400000m,
            MontoRecaudado = 4400000m,
            Estado = "Comprado",
            FechaCreacion = DateTime.UtcNow.AddDays(-15),
            PrecioCompra = 4250000m,
            FechaCompra = DateTime.UtcNow.AddDays(-5),
            PrecioVentaEstimado = 5200000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Citroen C3 2018",
            AutoDescripcion = "Citroen C3 2018, 1.2 nafta, 65.000 km, ubicado en Tucuman capital, service al dia.",
            MontoObjetivo = 3300000m,
            MontoRecaudado = 3300000m,
            Estado = "Comprado",
            FechaCreacion = DateTime.UtcNow.AddDays(-12),
            PrecioCompra = 3150000m,
            FechaCompra = DateTime.UtcNow.AddDays(-3),
            PrecioVentaEstimado = 3900000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Honda HR-V 2020",
            AutoDescripcion = "Honda HR-V 2020, 1.8 nafta CVT, 40.000 km, full, unico dueño, ubicado en Neuquen capital.",
            MontoObjetivo = 7600000m,
            MontoRecaudado = 7600000m,
            Estado = "Vendido",
            FechaCreacion = DateTime.UtcNow.AddDays(-75),
            PrecioCompra = 7300000m,
            FechaCompra = DateTime.UtcNow.AddDays(-45),
            PrecioVenta = 8800000m,
            FechaVenta = DateTime.UtcNow.AddDays(-6),
            PrecioVentaEstimado = 8800000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Jeep Compass 2018",
            AutoDescripcion = "Jeep Compass 2018, 2.4 nafta 4x2, 78.000 km, ubicado en Bahia Blanca, Buenos Aires.",
            MontoObjetivo = 6400000m,
            MontoRecaudado = 6400000m,
            Estado = "Vendido",
            FechaCreacion = DateTime.UtcNow.AddDays(-85),
            PrecioCompra = 6100000m,
            FechaCompra = DateTime.UtcNow.AddDays(-50),
            PrecioVenta = 7400000m,
            FechaVenta = DateTime.UtcNow.AddDays(-8),
            PrecioVentaEstimado = 7400000m,
            ImagenUrl = ImagenPlaceholder,
        });
        AgregarSiFalta(new Pozo
        {
            Titulo = "Renault Logan 2016",
            AutoDescripcion = "Renault Logan 2016, 1.6 nafta, 120.000 km, ubicado en Zarate, Buenos Aires, service oficial.",
            MontoObjetivo = 2900000m,
            MontoRecaudado = 2900000m,
            Estado = "Vendido",
            FechaCreacion = DateTime.UtcNow.AddDays(-95),
            PrecioCompra = 2750000m,
            FechaCompra = DateTime.UtcNow.AddDays(-60),
            PrecioVenta = 3400000m,
            FechaVenta = DateTime.UtcNow.AddDays(-4),
            PrecioVentaEstimado = 3400000m,
            ImagenUrl = ImagenPlaceholder,
        });

        if (nuevos.Count > 0)
        {
            db.Pozos.AddRange(nuevos);
            db.SaveChanges();
        }

        return pozoVwGol;
    }

    // Varias inversiones de ejemplo del admin, en pozos con estados distintos
    // (Abierto/Comprado/Vendido), para que la pantalla "Mis inversiones" se
    // vea con una grilla de varias tarjetas al entrar con las credenciales de
    // demo en vez de una sola. La primera (VW Gol Trend 2019) recibe el pozo
    // por parametro -el que devolvio SeedPozos- en vez de buscarlo por
    // Titulo, para no depender de que el Titulo sea unico; las demas se
    // buscan por Titulo con FirstOrDefault (no SingleOrDefault) por la misma
    // razon. Cada Monto es la mitad (o el total, para el VW Gol) del
    // MontoRecaudado ya sembrado en ese pozo (no lo modifica ni lo supera) y
    // cada Fecha es posterior a la FechaCreacion del pozo.
    // La siembra es ADITIVA: no corta si ya hay inversiones cargadas (por un
    // usuario real, o por una corrida anterior con esta misma lista mas
    // corta). Cada inversion se identifica por el par (UsuarioId, PozoId) -no
    // hay otro campo natural en el modelo- y solo se agrega si ese admin
    // todavia no tiene una inversion sembrada en ese pozo, para poder correr
    // esto varias veces sin duplicar nada.
    public static void SeedInversiones(AppDbContext db, Pozo? pozoVwGol)
    {
        var admin = db.Usuarios.SingleOrDefault(u => u.Email == AdminEmail);
        if (admin is null)
            return;

        var nuevas = new List<Inversion>();

        bool YaExiste(Guid pozoId) =>
            db.Inversiones.Any(i => i.UsuarioId == admin.Id && i.PozoId == pozoId) ||
            nuevas.Any(i => i.PozoId == pozoId);

        if (pozoVwGol is not null && !YaExiste(pozoVwGol.Id))
        {
            nuevas.Add(new Inversion
            {
                PozoId = pozoVwGol.Id,
                UsuarioId = admin.Id,
                Monto = pozoVwGol.MontoRecaudado,
                Fecha = pozoVwGol.FechaCompra ?? pozoVwGol.FechaCreacion,
            });
        }

        void AgregarSiFalta(string titulo, int diasDespuesDeCreado)
        {
            var pozo = db.Pozos.FirstOrDefault(p => p.Titulo == titulo);
            if (pozo is null || YaExiste(pozo.Id))
                return;

            nuevas.Add(new Inversion
            {
                PozoId = pozo.Id,
                UsuarioId = admin.Id,
                Monto = pozo.MontoRecaudado / 2,
                Fecha = pozo.FechaCreacion.AddDays(diasDespuesDeCreado),
            });
        }

        AgregarSiFalta("Fiat Cronos 2021", 0);
        AgregarSiFalta("Chevrolet Onix 2020", 1);
        AgregarSiFalta("Renault Sandero Stepway 2017", 2);
        AgregarSiFalta("Peugeot 208 2019", 3);
        AgregarSiFalta("Honda Civic 2020", 0);

        // 19 inversiones nuevas mas, en pozos distintos de los 6 de arriba
        // (VW Gol Trend + los 5 de encima), para que "Mis inversiones" pase
        // de 6 a 26 tarjetas y se vea como una grilla real en vez de una fila
        // corta.
        AgregarSiFalta("Toyota Corolla 2018", 5);
        AgregarSiFalta("Volkswagen Vento 2018", 1);
        AgregarSiFalta("Renault Kangoo 2020", 2);
        AgregarSiFalta("Nissan Kicks 2021", 3);
        AgregarSiFalta("Ford Ka 2017", 1);
        AgregarSiFalta("Toyota Hilux 2019", 4);
        AgregarSiFalta("Citroen C4 Cactus 2020", 2);
        AgregarSiFalta("Peugeot 308 2015", 5);
        AgregarSiFalta("Volkswagen Suran 2016", 5);
        AgregarSiFalta("Fiat Argo 2019", 10);
        AgregarSiFalta("Renault Duster 2018", 8);
        AgregarSiFalta("Chevrolet Cruze 2017", 1);
        AgregarSiFalta("Toyota Etios 2020", 2);
        AgregarSiFalta("Volkswagen Polo 2022", 0);
        AgregarSiFalta("Peugeot 3008 2019", 6);
        AgregarSiFalta("Nissan Versa 2021", 4);
        AgregarSiFalta("Citroen C3 2018", 3);
        AgregarSiFalta("Honda HR-V 2020", 10);
        AgregarSiFalta("Jeep Compass 2018", 12);
        AgregarSiFalta("Renault Logan 2016", 15);

        if (nuevas.Count > 0)
        {
            db.Inversiones.AddRange(nuevas);
            db.SaveChanges();
        }
    }
}
