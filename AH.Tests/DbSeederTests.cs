using AH.Api.Controllers;
using AH.Api.Data;
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
    public void SeedPozos_CreaLosTresPozosDeEjemploConSusEstados()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);

        Assert.Equal(3, db.Pozos.Count());
        Assert.Single(db.Pozos, p => p.Estado == "Abierto");
        Assert.Single(db.Pozos, p => p.Estado == "Comprado");
        Assert.Single(db.Pozos, p => p.Estado == "Vendido");
    }

    [Fact]
    public void SeedPozos_ElPozoComprado_TienePrecioYFechaDeCompra()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);

        var pozo = db.Pozos.Single(p => p.Estado == "Comprado");
        Assert.NotNull(pozo.PrecioCompra);
        Assert.NotNull(pozo.FechaCompra);
        Assert.Null(pozo.PrecioVenta);
        Assert.Null(pozo.FechaVenta);
    }

    [Fact]
    public void SeedPozos_ElPozoVendido_TienePrecioYFechaDeCompraYVenta()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);

        var pozo = db.Pozos.Single(p => p.Estado == "Vendido");
        Assert.NotNull(pozo.PrecioCompra);
        Assert.NotNull(pozo.FechaCompra);
        Assert.NotNull(pozo.PrecioVenta);
        Assert.NotNull(pozo.FechaVenta);
    }

    [Fact]
    public void SeedPozos_EsIdempotente_NoDuplicaSiSeLlamaDeNuevo()
    {
        var db = BuildDb();

        DbSeeder.SeedPozos(db);
        DbSeeder.SeedPozos(db);

        Assert.Equal(3, db.Pozos.Count());
    }
}
