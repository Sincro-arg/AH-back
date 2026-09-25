using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AH.Tests;

public class PozosControllerEscrituraTests
{
    private static (PozosController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pozos-escritura-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var ctrl = new PozosController(db);
        return (ctrl, db);
    }

    private static Pozo SeedPozo(AppDbContext db, string estado = "Abierto")
    {
        var pozo = new Pozo
        {
            Titulo = "Fiat Cronos 2021",
            AutoDescripcion = "Fiat Cronos 2021, 45.000 km, nafta, full.",
            MontoObjetivo = 3000000m,
            MontoRecaudado = 1200000m,
            Estado = estado,
        };
        db.Pozos.Add(pozo);
        db.SaveChanges();
        return pozo;
    }

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    // ---- POST /api/pozos ----

    [Fact]
    public async Task Crear_CaminoFeliz_DevuelvePozoAbiertoConMontoRecaudadoCero()
    {
        var (ctrl, db) = Build();

        var dto = new PozosController.CrearPozoDto("Peugeot 208 2020", "Peugeot 208 2020, nafta.", 2500000m);
        var res = await ctrl.Crear(dto);

        var created = Assert.IsType<ObjectResult>(res);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("Peugeot 208 2020", GetProp(created.Value!, "titulo"));
        Assert.Equal("Abierto", GetProp(created.Value!, "estado"));
        Assert.Equal(0m, GetProp(created.Value!, "montoRecaudado"));
        Assert.Equal(2500000m, GetProp(created.Value!, "montoObjetivo"));

        var pozo = Assert.Single(db.Pozos);
        Assert.Equal("Peugeot 208 2020", pozo.Titulo);
        Assert.Equal("Abierto", pozo.Estado);
    }

    [Fact]
    public async Task Crear_SinTitulo_Devuelve400()
    {
        var (ctrl, db) = Build();

        var dto = new PozosController.CrearPozoDto("   ", "descripcion", 100000m);
        var res = await ctrl.Crear(dto);

        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Empty(db.Pozos);
    }

    [Fact]
    public async Task Crear_MontoObjetivoNoPositivo_Devuelve400()
    {
        var (ctrl, db) = Build();

        var dto = new PozosController.CrearPozoDto("Auto", "descripcion", 0m);
        var res = await ctrl.Crear(dto);

        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Empty(db.Pozos);
    }

    // ---- PUT /api/pozos/{id} ----

    [Fact]
    public async Task Actualizar_CaminoFeliz_ModificaLosDatosDelPozo()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db);

        var dto = new PozosController.ActualizarPozoDto("Fiat Cronos 2022", "Otra descripcion", 3500000m);
        var res = await ctrl.Actualizar(pozo.Id, dto);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal("Fiat Cronos 2022", GetProp(ok.Value!, "titulo"));
        Assert.Equal(3500000m, GetProp(ok.Value!, "montoObjetivo"));

        var actualizado = await db.Pozos.FindAsync(pozo.Id);
        Assert.Equal("Fiat Cronos 2022", actualizado!.Titulo);
        Assert.Equal(3500000m, actualizado.MontoObjetivo);
    }

    [Fact]
    public async Task Actualizar_PozoNoAbierto_Devuelve409()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db, estado: "Comprado");

        var dto = new PozosController.ActualizarPozoDto("Nuevo titulo", "Nueva descripcion", 1000000m);
        var res = await ctrl.Actualizar(pozo.Id, dto);

        Assert.IsType<ConflictObjectResult>(res);
        var sinCambios = await db.Pozos.FindAsync(pozo.Id);
        Assert.Equal("Fiat Cronos 2021", sinCambios!.Titulo);
    }

    [Fact]
    public async Task Actualizar_PozoInexistente_Devuelve404()
    {
        var (ctrl, _) = Build();

        var dto = new PozosController.ActualizarPozoDto("Titulo", "Descripcion", 100000m);
        var res = await ctrl.Actualizar(Guid.NewGuid(), dto);

        Assert.IsType<NotFoundObjectResult>(res);
    }

    // ---- DELETE /api/pozos/{id} ----

    [Fact]
    public async Task Eliminar_CaminoFeliz_BorraElPozo()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db);

        var res = await ctrl.Eliminar(pozo.Id);

        Assert.IsType<NoContentResult>(res);
        Assert.Empty(db.Pozos);
    }

    [Fact]
    public async Task Eliminar_PozoConInversiones_Devuelve409()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db);
        var usuario = new Usuario
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Email = "inversor@example.com",
            Telefono = "1122334455",
            PasswordHash = "hash",
        };
        db.Usuarios.Add(usuario);
        db.Inversiones.Add(new Inversion { PozoId = pozo.Id, UsuarioId = usuario.Id, Monto = 500000m });
        db.SaveChanges();

        var res = await ctrl.Eliminar(pozo.Id);

        Assert.IsType<ConflictObjectResult>(res);
        Assert.Single(db.Pozos);
    }

    [Fact]
    public async Task Eliminar_PozoInexistente_Devuelve404()
    {
        var (ctrl, _) = Build();

        var res = await ctrl.Eliminar(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(res);
    }
}
