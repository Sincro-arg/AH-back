using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace AH.Tests;

public class InversionesControllerTests
{
    private static (InversionesController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"inversiones-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var ctrl = new InversionesController(db);
        return (ctrl, db);
    }

    private static void AutenticarComo(InversionesController ctrl, Guid userId)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
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

    private static Pozo SeedPozo(AppDbContext db, string estado = "Abierto", decimal montoRecaudado = 0m)
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

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    // ---- POST /api/pozos/{id}/inversiones ----

    [Fact]
    public async Task Crear_CaminoFeliz_CreaLaInversionYRecalculaMontoRecaudado()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, montoRecaudado: 100000m);
        var usuario = SeedUsuario(db, "inversor@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new InversionesController.CrearInversionDto(50000m);
        var res = await ctrl.Crear(pozo.Id, dto);

        var created = Assert.IsType<ObjectResult>(res);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(pozo.Id.ToString(), GetProp(created.Value!, "pozoId"));
        Assert.Equal(usuario.Id.ToString(), GetProp(created.Value!, "usuarioId"));
        Assert.Equal(50000m, GetProp(created.Value!, "monto"));

        var inversion = Assert.Single(db.Inversiones);
        Assert.Equal(50000m, inversion.Monto);
        Assert.Equal(usuario.Id, inversion.UsuarioId);

        var pozoActualizado = await db.Pozos.FindAsync(pozo.Id);
        Assert.Equal(150000m, pozoActualizado!.MontoRecaudado);
    }

    [Fact]
    public async Task Crear_PozoNoAbierto_Devuelve409YNoCreaNada()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Comprado", montoRecaudado: 100000m);
        var usuario = SeedUsuario(db, "inversor@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new InversionesController.CrearInversionDto(50000m);
        var res = await ctrl.Crear(pozo.Id, dto);

        var conflict = Assert.IsType<ConflictObjectResult>(res);
        Assert.Equal("El pozo no está abierto para recibir inversiones", GetProp(conflict.Value!, "error"));

        Assert.Empty(db.Inversiones);
        var pozoSinCambios = await db.Pozos.FindAsync(pozo.Id);
        Assert.Equal(100000m, pozoSinCambios!.MontoRecaudado);
    }

    [Fact]
    public async Task Crear_MontoNoPositivo_Devuelve400()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db);
        var usuario = SeedUsuario(db, "inversor@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new InversionesController.CrearInversionDto(0m);
        var res = await ctrl.Crear(pozo.Id, dto);

        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Empty(db.Inversiones);
    }

    [Fact]
    public async Task Crear_PozoInexistente_Devuelve404()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db, "inversor@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new InversionesController.CrearInversionDto(50000m);
        var res = await ctrl.Crear(Guid.NewGuid(), dto);

        Assert.IsType<NotFoundObjectResult>(res);
    }

    // ---- GET /api/pozos/{id}/inversiones ----

    [Fact]
    public async Task Listar_DevuelveLasInversionesDelPozoConNombreDelInversor()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db);
        var usuario = SeedUsuario(db, "inversor@example.com");
        db.Inversiones.Add(new Inversion { PozoId = pozo.Id, UsuarioId = usuario.Id, Monto = 50000m });
        db.SaveChanges();
        AutenticarComo(ctrl, usuario.Id);

        var res = await ctrl.Listar(pozo.Id);

        var ok = Assert.IsType<OkObjectResult>(res);
        var lista = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
        Assert.Single(lista);
        Assert.Equal("Juan Perez", GetProp(lista[0], "nombreInversor"));
        Assert.Equal(50000m, GetProp(lista[0], "monto"));
    }

    [Fact]
    public async Task Listar_PozoInexistente_Devuelve404()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db, "inversor@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var res = await ctrl.Listar(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(res);
    }
}
