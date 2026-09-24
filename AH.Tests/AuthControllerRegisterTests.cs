using AH.Api.Controllers;
using AH.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AH.Tests;

public class AuthControllerRegisterTests
{
    private static (AuthController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"register-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave-de-test-super-larga-para-firmar-1234567890",
                ["Jwt:Issuer"] = "AH.Api.Test",
                ["Jwt:Audience"] = "AH.App.Test",
                ["Jwt:ExpireHours"] = "8",
            })
            .Build();
        var ctrl = new AuthController(db, config);
        return (ctrl, db);
    }

    private static AuthController.RegisterDto DtoValido(string email = "juan.perez@example.com") =>
        new("Juan", "Perez", email, "1122334455", "unaPassword1");

    [Fact]
    public async Task RegistroExitoso_CreaElUsuarioYDevuelveOk()
    {
        var (ctrl, db) = Build();

        var res = await ctrl.Register(DtoValido());

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.NotNull(ok.Value);

        var usuario = Assert.Single(db.Usuarios);
        Assert.Equal("juan.perez@example.com", usuario.Email);
        Assert.Equal("Juan", usuario.Nombre);
        Assert.Equal("Perez", usuario.Apellido);
        Assert.NotEqual("unaPassword1", usuario.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("unaPassword1", usuario.PasswordHash));
    }

    [Fact]
    public async Task EmailDuplicado_Devuelve409()
    {
        var (ctrl, db) = Build();
        await ctrl.Register(DtoValido());

        var res = await ctrl.Register(DtoValido());

        Assert.IsType<ConflictObjectResult>(res);
        Assert.Single(db.Usuarios);
    }

    [Fact]
    public async Task PasswordCorta_Devuelve400()
    {
        var (ctrl, db) = Build();

        var dto = DtoValido() with { Password = "corta1" };
        var res = await ctrl.Register(dto);

        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Empty(db.Usuarios);
    }

    [Fact]
    public async Task EmailInvalido_Devuelve400()
    {
        var (ctrl, db) = Build();

        var dto = DtoValido() with { Email = "no-es-un-email" };
        var res = await ctrl.Register(dto);

        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Empty(db.Usuarios);
    }
}
