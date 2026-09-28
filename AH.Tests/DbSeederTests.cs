using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AH.Tests;

public class DbSeederTests
{
    private static AppDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"seed-{Guid.NewGuid()}")
            .Options);

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave-de-test-super-larga-para-firmar-1234567890",
                ["Jwt:Issuer"] = "AH.Api.Test",
                ["Jwt:Audience"] = "AH.App.Test",
                ["Jwt:ExpireHours"] = "8",
            })
            .Build();

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    [Fact]
    public void SeedAdminUsuario_CreaElUsuarioDePruebaConLaPasswordDelPliego()
    {
        var db = BuildDb();

        DbSeeder.SeedAdminUsuario(db);

        var usuario = db.Usuarios.Single(u => u.Email == DbSeeder.AdminEmail);
        Assert.True(BCrypt.Net.BCrypt.Verify(DbSeeder.AdminPassword, usuario.PasswordHash));
    }

    [Fact]
    public void SeedAdminUsuario_EsIdempotente_NoDuplicaSiSeLlamaDeNuevo()
    {
        var db = BuildDb();

        DbSeeder.SeedAdminUsuario(db);
        DbSeeder.SeedAdminUsuario(db);

        Assert.Equal(1, db.Usuarios.Count(u => u.Email == DbSeeder.AdminEmail));
    }

    [Fact]
    public async Task AdminSembrado_PuedeLoguearseYRecibirToken()
    {
        var db = BuildDb();
        DbSeeder.SeedAdminUsuario(db);
        var ctrl = new AuthController(db, BuildConfig());

        var res = await ctrl.Login(new AuthController.LoginDto(DbSeeder.AdminEmail, DbSeeder.AdminPassword));

        var ok = Assert.IsType<OkObjectResult>(res);
        var token = GetProp(ok.Value!, "token") as string;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var usuarioDto = GetProp(ok.Value!, "usuario");
        Assert.Equal(DbSeeder.AdminEmail, GetProp(usuarioDto!, "email"));
    }

    [Fact]
    public void SeedPozos_CreaPozosDeEjemploConEstadosVariados()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);

        Assert.Equal(31, db.Pozos.Count());
        Assert.Equal(17, db.Pozos.Count(p => p.Estado == "Abierto"));
        Assert.Equal(7, db.Pozos.Count(p => p.Estado == "Comprado"));
        Assert.Equal(7, db.Pozos.Count(p => p.Estado == "Vendido"));
    }

    [Fact]
    public void SeedPozos_LosPozosCompradosTienenPrecioYFechaDeCompraYNoTienenVenta()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);

        Assert.All(db.Pozos.Where(p => p.Estado == "Comprado"), pozo =>
        {
            Assert.NotNull(pozo.PrecioCompra);
            Assert.NotNull(pozo.FechaCompra);
            Assert.Null(pozo.PrecioVenta);
            Assert.Null(pozo.FechaVenta);
        });
    }

    [Fact]
    public void SeedPozos_LosPozosVendidosTienenPrecioYFechaDeCompraYVenta()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);

        Assert.All(db.Pozos.Where(p => p.Estado == "Vendido"), pozo =>
        {
            Assert.NotNull(pozo.PrecioCompra);
            Assert.NotNull(pozo.FechaCompra);
            Assert.NotNull(pozo.PrecioVenta);
            Assert.NotNull(pozo.FechaVenta);
        });
    }

    [Fact]
    public void SeedPozos_TodosTienenImagenYLosAbiertosTienenPrecioVentaEstimado()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);

        Assert.All(db.Pozos, p => Assert.False(string.IsNullOrWhiteSpace(p.ImagenUrl)));

        Assert.All(db.Pozos.Where(p => p.Estado == "Abierto"), abierto =>
            Assert.NotNull(abierto.PrecioVentaEstimado));
    }

    [Fact]
    public void SeedPozos_EsIdempotente_NoDuplicaSiSeLlamaDeNuevo()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);
        DbSeeder.SeedPozos(db);

        Assert.Equal(31, db.Pozos.Count());
    }

    [Fact]
    public void SeedPozos_EsAditiva_CompletaLosQueFaltanSinDuplicarLosQueYaEstaban()
    {
        var db = BuildDb();

        // Simula una corrida anterior con la lista vieja mas corta: ya habia
        // dos de los pozos de ejemplo cargados (uno con exactamente los
        // mismos datos que carga SeedPozos, otro con datos propios, como si
        // lo hubiera tocado un usuario), pero todavia no existia el resto de
        // la lista actual.
        db.Pozos.AddRange(
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
                ImagenUrl = "https://cdn.example.com/pozos/vw-gol-trend-2019.jpg",
            },
            new Pozo
            {
                Titulo = "Fiat Cronos 2021",
                AutoDescripcion = "Descripcion vieja, cargada por una corrida anterior.",
                MontoObjetivo = 3000000m,
                MontoRecaudado = 500000m,
                Estado = "Abierto",
                FechaCreacion = DateTime.UtcNow.AddDays(-100),
                ImagenUrl = "https://cdn.example.com/pozos/fiat-cronos-2021-viejo.jpg",
            }
        );
        db.SaveChanges();

        var pozoVwGol = DbSeeder.SeedPozos(db);

        Assert.Equal(31, db.Pozos.Count());
        Assert.Equal(1, db.Pozos.Count(p => p.Titulo == "VW Gol Trend 2019"));
        Assert.Equal(1, db.Pozos.Count(p => p.Titulo == "Fiat Cronos 2021"));

        // El que ya estaba no se toco ni se reemplazo.
        var fiatCronos = db.Pozos.Single(p => p.Titulo == "Fiat Cronos 2021");
        Assert.Equal("Descripcion vieja, cargada por una corrida anterior.", fiatCronos.AutoDescripcion);
        Assert.Equal(500000m, fiatCronos.MontoRecaudado);

        // Un pozo que faltaba se completo.
        Assert.Equal(1, db.Pozos.Count(p => p.Titulo == "Toyota Hilux 2019"));
        // Uno de los 10 nuevos tambien se completo.
        Assert.Equal(1, db.Pozos.Count(p => p.Titulo == "Renault Logan 2016"));

        Assert.NotNull(pozoVwGol);
        Assert.Equal("VW Gol Trend 2019", pozoVwGol!.Titulo);
    }

    [Fact]
    public void SeedInversiones_NoExplotaConDosPozosDeIgualTitulo_YAsociaLaInversionAlPozoSembradoOriginalmente()
    {
        var db = BuildDb();
        DbSeeder.SeedAdminUsuario(db);
        var pozoOriginal = DbSeeder.SeedPozos(db);
        Assert.NotNull(pozoOriginal);

        // Un segundo pozo con el mismo Titulo, cargado por fuera de SeedPozos
        // (por ejemplo por un usuario), para reproducir la colision que antes
        // hacia reventar el SingleOrDefault por Titulo dentro de SeedInversiones.
        db.Pozos.Add(new Pozo
        {
            Titulo = "VW Gol Trend 2019",
            AutoDescripcion = "Otro VW Gol Trend, cargado despues por un usuario.",
            MontoObjetivo = 1000000m,
            MontoRecaudado = 500000m,
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow,
        });
        db.SaveChanges();
        Assert.Equal(2, db.Pozos.Count(p => p.Titulo == "VW Gol Trend 2019"));

        var excepcion = Record.Exception(() => DbSeeder.SeedInversiones(db, pozoOriginal));

        Assert.Null(excepcion);
        Assert.Equal(6, db.Inversiones.Count());
        Assert.Contains(db.Inversiones, i => i.PozoId == pozoOriginal!.Id);

        var pozoDuplicadoId = db.Pozos.Single(p => p.Titulo == "VW Gol Trend 2019" && p.Id != pozoOriginal!.Id).Id;
        Assert.DoesNotContain(db.Inversiones, i => i.PozoId == pozoDuplicadoId);
    }

    [Fact]
    public void SeedInversiones_EsIdempotente_NoDuplicaSiSeLlamaDeNuevo()
    {
        var db = BuildDb();
        DbSeeder.SeedAdminUsuario(db);
        var pozoVwGol = DbSeeder.SeedPozos(db);

        DbSeeder.SeedInversiones(db, pozoVwGol);
        DbSeeder.SeedInversiones(db, pozoVwGol);

        Assert.Equal(6, db.Inversiones.Count());
    }

    [Fact]
    public void SeedInversiones_TodasApuntanAPozosValidosYNoSuperanElMontoRecaudado()
    {
        var db = BuildDb();
        DbSeeder.SeedAdminUsuario(db);
        var pozoVwGol = DbSeeder.SeedPozos(db);

        DbSeeder.SeedInversiones(db, pozoVwGol);

        Assert.All(db.Inversiones, inversion =>
        {
            var pozo = db.Pozos.SingleOrDefault(p => p.Id == inversion.PozoId);
            Assert.NotNull(pozo);
            Assert.True(inversion.Monto <= pozo!.MontoRecaudado);
        });
    }
}
