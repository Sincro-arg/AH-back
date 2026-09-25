using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace AH.Tests;

public class PozosControllerGetByIdTests
{
    private static (PozosController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pozos-getbyid-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var ctrl = new PozosController(db);
        return (ctrl, db);
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

    private static Pozo SeedPozo(AppDbContext db)
    {
        var pozo = new Pozo
        {
            Titulo = "Fiat Cronos 2021",
            AutoDescripcion = "Fiat Cronos 2021, 45.000 km, nafta, full.",
            MontoObjetivo = 3000000m,
            MontoRecaudado = 1200000m,
            Estado = "Abierto",
        };
        db.Pozos.Add(pozo);
        db.SaveChanges();
        return pozo;
    }

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    [Fact]
    public async Task PozoExistente_DevuelvePozoConSusInversiones()
    {
        var (ctrl, db) = Build();
        var pozo = SeedPozo(db);
        var usuario = SeedUsuario(db, "inversor@example.com");
        var inversion = new Inversion
        {
            PozoId = pozo.Id,
            UsuarioId = usuario.Id,
            Monto = 500000m,
        };
        db.Inversiones.Add(inversion);
        db.SaveChanges();

        var res = await ctrl.ObtenerPorId(pozo.Id);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal(pozo.Id.ToString(), GetProp(ok.Value!, "id"));
        Assert.Equal(pozo.Titulo, GetProp(ok.Value!, "titulo"));
        Assert.Equal(pozo.Estado, GetProp(ok.Value!, "estado"));

        var inversiones = GetProp(ok.Value!, "inversiones") as System.Collections.IEnumerable;
        Assert.NotNull(inversiones);
        var lista = inversiones!.Cast<object>().ToList();
        Assert.Single(lista);
        Assert.Equal(inversion.Id.ToString(), GetProp(lista[0], "id"));
        Assert.Equal(usuario.Id.ToString(), GetProp(lista[0], "usuarioId"));
        Assert.Equal("Juan Perez", GetProp(lista[0], "nombreInversor"));
        Assert.Equal(500000m, GetProp(lista[0], "monto"));
    }

    [Fact]
    public async Task PozoInexistente_Devuelve404ConMensaje()
    {
        var (ctrl, _) = Build();

        var res = await ctrl.ObtenerPorId(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(res);
        Assert.Equal("El pozo no existe", GetProp(notFound.Value!, "error"));
    }
}
