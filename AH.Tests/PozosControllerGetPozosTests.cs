using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AH.Tests;

public class PozosControllerGetPozosTests
{
    private static (PozosController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pozos-get-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var ctrl = new PozosController(db);
        return (ctrl, db);
    }

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    [Fact]
    public async Task GetPozos_DevuelveImagenUrlYPrecioVentaEstimado()
    {
        var (ctrl, db) = Build();
        db.Pozos.Add(new Pozo
        {
            Titulo = "Fiat Cronos 2020",
            AutoDescripcion = "Fiat Cronos 2020, muy poco uso.",
            MontoObjetivo = 5000000m,
            MontoRecaudado = 1000000m,
            Estado = "Abierto",
            ImagenUrl = "https://images.unsplash.com/photo-test?w=800&q=80",
            PrecioVentaEstimado = 6100000m,
        });
        db.SaveChanges();

        var res = await ctrl.GetPozos();

        var ok = Assert.IsType<OkObjectResult>(res);
        var pozo = Assert.Single((IEnumerable<object>)ok.Value!);
        Assert.Equal("https://images.unsplash.com/photo-test?w=800&q=80", GetProp(pozo, "imagenUrl"));
        Assert.Equal(6100000m, GetProp(pozo, "precioVentaEstimado"));
    }

    [Fact]
    public async Task GetPozos_ConCamposNulos_DevuelveNull()
    {
        var (ctrl, db) = Build();
        db.Pozos.Add(new Pozo
        {
            Titulo = "Renault Sandero 2019",
            AutoDescripcion = "Renault Sandero 2019, sin detalles.",
            MontoObjetivo = 4000000m,
            MontoRecaudado = 0m,
            Estado = "Abierto",
        });
        db.SaveChanges();

        var res = await ctrl.GetPozos();

        var ok = Assert.IsType<OkObjectResult>(res);
        var pozo = Assert.Single((IEnumerable<object>)ok.Value!);
        Assert.Null(GetProp(pozo, "imagenUrl"));
        Assert.Null(GetProp(pozo, "precioVentaEstimado"));
    }
}
