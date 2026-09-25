using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace AH.Tests;

public class PozosEstadoYRepartoTests
{
    private static (PozosController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pozos-estado-reparto-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var ctrl = new PozosController(db);
        return (ctrl, db);
    }

    private static Pozo SeedPozo(AppDbContext db, string estado = "Abierto", decimal montoRecaudado = 1200000m)
    {
        var pozo = new Pozo
        {
            Titulo = "Fiat Cronos 2021",
            AutoDescripcion = "Fiat Cronos 2021, 45.000 km, nafta, full.",
            MontoObjetivo = 3000000m,
            MontoRecaudado = montoRecaudado,
            Estado = estado,
        };
        db.Pozos.Add(pozo);
        db.SaveChanges();
        return pozo;
    }

    private static Usuario SeedUsuario(AppDbContext db, string email)
    {
        var usuario = new Usuario
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Email = email,
            Telefono = "1122334455",
            PasswordHash = "hash",
        };
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        return usuario;
    }

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    // ---- PUT /api/pozos/{id}/estado ----

    [Fact]
    public async Task MarcarComprado_CaminoFeliz_PasaAComprado()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Abierto");

        var dto = new PozosController.CambiarEstadoDto("marcarComprado", 2000000m, new DateTime(2026, 1, 10), null, null);
        var res = await ctrl.CambiarEstado(pozo.Id, dto);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal("Comprado", GetProp(ok.Value!, "estado"));
        Assert.Equal(2000000m, GetProp(ok.Value!, "precioCompra"));

        var actualizado = await db.Pozos.FindAsync(pozo.Id);
        Assert.Equal("Comprado", actualizado!.Estado);
        Assert.Equal(2000000m, actualizado.PrecioCompra);
    }

    [Fact]
    public async Task MarcarVendido_CaminoFeliz_PasaAVendido()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Comprado");
        pozo.PrecioCompra = 2000000m;
        pozo.FechaCompra = new DateTime(2026, 1, 10);
        db.SaveChanges();

        var dto = new PozosController.CambiarEstadoDto("marcarVendido", null, null, 2600000m, new DateTime(2026, 3, 5));
        var res = await ctrl.CambiarEstado(pozo.Id, dto);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal("Vendido", GetProp(ok.Value!, "estado"));
        Assert.Equal(2600000m, GetProp(ok.Value!, "precioVenta"));

        var actualizado = await db.Pozos.FindAsync(pozo.Id);
        Assert.Equal("Vendido", actualizado!.Estado);
        Assert.Equal(2600000m, actualizado.PrecioVenta);
    }

    [Fact]
    public async Task MarcarComprado_PozoNoAbierto_Devuelve409()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Comprado");

        var dto = new PozosController.CambiarEstadoDto("marcarComprado", 2000000m, new DateTime(2026, 1, 10), null, null);
        var res = await ctrl.CambiarEstado(pozo.Id, dto);

        Assert.IsType<ConflictObjectResult>(res);
    }

    [Fact]
    public async Task MarcarComprado_SinPrecioNiFecha_Devuelve400()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Abierto");

        var dto = new PozosController.CambiarEstadoDto("marcarComprado", null, null, null, null);
        var res = await ctrl.CambiarEstado(pozo.Id, dto);

        Assert.IsType<BadRequestObjectResult>(res);
    }

    [Fact]
    public async Task MarcarVendido_SinPrecioNiFecha_Devuelve400()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Comprado");
        pozo.PrecioCompra = 2000000m;
        pozo.FechaCompra = new DateTime(2026, 1, 10);
        db.SaveChanges();

        var dto = new PozosController.CambiarEstadoDto("marcarVendido", null, null, null, null);
        var res = await ctrl.CambiarEstado(pozo.Id, dto);

        Assert.IsType<BadRequestObjectResult>(res);
    }

    [Fact]
    public async Task MarcarVendido_PozoNoComprado_Devuelve409()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Abierto");

        var dto = new PozosController.CambiarEstadoDto("marcarVendido", null, null, 2600000m, new DateTime(2026, 3, 5));
        var res = await ctrl.CambiarEstado(pozo.Id, dto);

        Assert.IsType<ConflictObjectResult>(res);
    }

    [Fact]
    public async Task CambiarEstado_AccionInvalida_Devuelve400()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Abierto");

        var dto = new PozosController.CambiarEstadoDto("marcarLoQueSea", null, null, null, null);
        var res = await ctrl.CambiarEstado(pozo.Id, dto);

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ---- GET /api/pozos/{id}/reparto ----

    [Fact]
    public async Task Reparto_CaminoFeliz_CalculaGananciaProporcional()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Vendido", montoRecaudado: 1000000m);
        pozo.PrecioCompra = 2000000m;
        pozo.PrecioVenta = 2500000m;
        db.SaveChanges();

        var usuario1 = SeedUsuario(db, "inversor1@example.com");
        var usuario2 = SeedUsuario(db, "inversor2@example.com");
        db.Inversiones.Add(new Inversion { PozoId = pozo.Id, UsuarioId = usuario1.Id, Monto = 700000m });
        db.Inversiones.Add(new Inversion { PozoId = pozo.Id, UsuarioId = usuario2.Id, Monto = 300000m });
        db.SaveChanges();

        var res = await ctrl.Reparto(pozo.Id);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal(500000m, GetProp(ok.Value!, "gananciaTotal"));

        var reparto = (GetProp(ok.Value!, "reparto") as System.Collections.IEnumerable)!.Cast<object>().ToList();
        Assert.Equal(2, reparto.Count);
        Assert.Equal(350000m, GetProp(reparto[0], "ganancia"));
        Assert.Equal(150000m, GetProp(reparto[1], "ganancia"));
    }

    [Fact]
    public async Task Reparto_PozoNoVendido_Devuelve409()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Comprado");

        var res = await ctrl.Reparto(pozo.Id);

        Assert.IsType<ConflictObjectResult>(res);
    }
}
