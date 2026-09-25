using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;

namespace AH.Tests;

public class PozosControllerListarTests
{
    private static (PozosController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pozos-listar-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var ctrl = new PozosController(db);
        return (ctrl, db);
    }

    private static Pozo SeedPozo(AppDbContext db, string titulo, string estado = "Abierto")
    {
        var pozo = new Pozo
        {
            Titulo = titulo,
            AutoDescripcion = $"{titulo}, 45.000 km, nafta, full.",
            MontoObjetivo = 3000000m,
            MontoRecaudado = 1200000m,
            Estado = estado,
        };
        db.Pozos.Add(pozo);
        db.SaveChanges();
        return pozo;
    }

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    [Fact]
    public async Task ConPozosCargados_DevuelveListadoConSusCampos()
    {
        var (ctrl, db) = Build();
        var pozo1 = SeedPozo(db, "Fiat Cronos 2021");
        var pozo2 = SeedPozo(db, "VW Gol Trend 2019");

        var res = await ctrl.Listar();

        var ok = Assert.IsType<OkObjectResult>(res);
        var lista = (ok.Value as System.Collections.IEnumerable)!.Cast<object>().ToList();
        Assert.Equal(2, lista.Count);

        var item1 = lista.Single(o => (string)GetProp(o, "id")! == pozo1.Id.ToString());
        Assert.Equal(pozo1.Titulo, GetProp(item1, "titulo"));
        Assert.Equal(pozo1.AutoDescripcion, GetProp(item1, "autoDescripcion"));
        Assert.Equal(pozo1.MontoObjetivo, GetProp(item1, "montoObjetivo"));
        Assert.Equal(pozo1.MontoRecaudado, GetProp(item1, "montoRecaudado"));
        Assert.Equal(pozo1.Estado, GetProp(item1, "estado"));
        Assert.Null(GetProp(item1, "precioCompra"));
        Assert.Null(GetProp(item1, "precioVenta"));

        var item2 = lista.Single(o => (string)GetProp(o, "id")! == pozo2.Id.ToString());
        Assert.Equal(pozo2.Titulo, GetProp(item2, "titulo"));
    }

    [Fact]
    public async Task SinPozos_DevuelveListaVacia()
    {
        var (ctrl, _) = Build();

        var res = await ctrl.Listar();

        var ok = Assert.IsType<OkObjectResult>(res);
        var lista = (ok.Value as System.Collections.IEnumerable)!.Cast<object>().ToList();
        Assert.Empty(lista);
    }
}
