using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace AH.Tests;

public class AuthControllerLoginTests
{
    private static (AuthController ctrl, AppDbContext db) Build(string expireHours = "8")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"login-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave-de-test-super-larga-para-firmar-1234567890",
                ["Jwt:Issuer"] = "AH.Api.Test",
                ["Jwt:Audience"] = "AH.App.Test",
                ["Jwt:ExpireHours"] = expireHours,
            })
            .Build();
        var ctrl = new AuthController(db, config);
        return (ctrl, db);
    }

    private static Usuario SeedUsuario(AppDbContext db, string email = "juan.perez@example.com", string password = "unaPassword1")
    {
        var usuario = new Usuario
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Email = email,
            Telefono = "1122334455",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        return usuario;
    }

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    [Fact]
    public async Task LoginCorrecto_DevuelveTokenYUsuario()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db);

        var res = await ctrl.Login(new AuthController.LoginDto(usuario.Email, "unaPassword1"));

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.NotNull(ok.Value);

        var token = GetProp(ok.Value!, "token") as string;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var usuarioDto = GetProp(ok.Value!, "usuario");
        Assert.NotNull(usuarioDto);
        Assert.Equal(usuario.Id.ToString(), GetProp(usuarioDto!, "id"));
        Assert.Equal(usuario.Email, GetProp(usuarioDto!, "email"));
        Assert.Equal(usuario.Nombre, GetProp(usuarioDto!, "nombre"));
        Assert.Equal(usuario.Apellido, GetProp(usuarioDto!, "apellido"));
        Assert.Equal(usuario.Telefono, GetProp(usuarioDto!, "telefono"));
        Assert.Equal(usuario.Tema, GetProp(usuarioDto!, "tema"));
        Assert.False(string.IsNullOrWhiteSpace(GetProp(usuarioDto!, "fechaAlta") as string));
        Assert.Equal(usuario.NotificacionesEmail, GetProp(usuarioDto!, "notificacionesEmail"));
    }

    [Fact]
    public async Task LoginCorrecto_TokenExpiraA24HsSegunPliego()
    {
        const int expireHours = 24;
        var (ctrl, db) = Build(expireHours.ToString());
        var usuario = SeedUsuario(db);
        var antesDeLoguear = DateTime.UtcNow;

        var res = await ctrl.Login(new AuthController.LoginDto(usuario.Email, "unaPassword1"));

        var ok = Assert.IsType<OkObjectResult>(res);
        var token = GetProp(ok.Value!, "token") as string;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var exp = jwt.ValidTo;

        var expiracionEsperada = antesDeLoguear.AddHours(expireHours);
        Assert.True(
            Math.Abs((exp - expiracionEsperada).TotalSeconds) < 5,
            $"Se esperaba que el token expire ~{expiracionEsperada:o} (ExpireHours={expireHours}), pero expira {exp:o}");
    }

    [Fact]
    public async Task EmailInexistente_Devuelve401ConMensajeGenerico()
    {
        var (ctrl, _) = Build();

        var res = await ctrl.Login(new AuthController.LoginDto("no-existe@example.com", "cualquierPassword1"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(res);
        Assert.Equal("Email o contraseña incorrectos", GetProp(unauthorized.Value!, "error"));
    }

    [Fact]
    public async Task PasswordIncorrecta_Devuelve401ConMismoMensaje()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db);

        var res = await ctrl.Login(new AuthController.LoginDto(usuario.Email, "passwordEquivocada1"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(res);
        Assert.Equal("Email o contraseña incorrectos", GetProp(unauthorized.Value!, "error"));
    }
}
