using AH.Api.Controllers;
using AH.Api.Data;
using AH.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace AH.Tests;

public class UsuariosControllerMeTests
{
    private static (UsuariosController ctrl, AppDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"usuarios-me-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        var ctrl = new UsuariosController(db);
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

    private static void AutenticarComo(UsuariosController ctrl, Guid userId)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }

    private static object? GetProp(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj);

    [Fact]
    public async Task SinToken_Devuelve401()
    {
        var (ctrl, _) = Build();
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        var res = await ctrl.GetMe();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(res);
        Assert.Equal("Token inválido", GetProp(unauthorized.Value!, "error"));
    }

    [Fact]
    public async Task ConToken_DevuelveSoloLosDatosDelUsuarioAutenticado()
    {
        var (ctrl, db) = Build();
        var usuarioA = SeedUsuario(db, "a@example.com");
        var usuarioB = SeedUsuario(db, "b@example.com");

        AutenticarComo(ctrl, usuarioA.Id);
        var res = await ctrl.GetMe();

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal(usuarioA.Id.ToString(), GetProp(ok.Value!, "id"));
        Assert.Equal(usuarioA.Email, GetProp(ok.Value!, "email"));
        Assert.Equal(usuarioA.Nombre, GetProp(ok.Value!, "nombre"));
        Assert.NotEqual(usuarioB.Id.ToString(), GetProp(ok.Value!, "id"));
        Assert.NotEqual(usuarioB.Email, GetProp(ok.Value!, "email"));
    }

    [Fact]
    public async Task PutMe_ConDatosValidos_ActualizaYDevuelveLosNuevosDatos()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db, "original@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new UsuariosController.UpdateMeDto("Nuevo", "Apellido", "5599887766", "nuevo@example.com");
        var res = await ctrl.UpdateMe(dto);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal("Nuevo", GetProp(ok.Value!, "nombre"));
        Assert.Equal("Apellido", GetProp(ok.Value!, "apellido"));
        Assert.Equal("nuevo@example.com", GetProp(ok.Value!, "email"));
        Assert.Equal("5599887766", GetProp(ok.Value!, "telefono"));

        var enDb = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Equal("nuevo@example.com", enDb!.Email);
    }

    [Fact]
    public async Task PutMe_ConEmailDeOtroUsuario_Devuelve409()
    {
        var (ctrl, db) = Build();
        var usuarioA = SeedUsuario(db, "a@example.com");
        var usuarioB = SeedUsuario(db, "b@example.com");
        AutenticarComo(ctrl, usuarioA.Id);

        var dto = new UsuariosController.UpdateMeDto("Juan", "Perez", "1122334455", usuarioB.Email);
        var res = await ctrl.UpdateMe(dto);

        var conflict = Assert.IsType<ConflictObjectResult>(res);
        Assert.Equal("El email ya esta registrado", GetProp(conflict.Value!, "error"));
    }

    [Fact]
    public async Task PutMe_NoPuedeEditarDatosDeOtroUsuario()
    {
        var (ctrl, db) = Build();
        var usuarioA = SeedUsuario(db, "a@example.com");
        var usuarioB = SeedUsuario(db, "b@example.com");
        AutenticarComo(ctrl, usuarioA.Id);

        var dto = new UsuariosController.UpdateMeDto("Hackeado", "Hackeado", "0000000000", "a@example.com");
        await ctrl.UpdateMe(dto);

        var bEnDb = await db.Usuarios.FindAsync(usuarioB.Id);
        Assert.Equal("Juan", bEnDb!.Nombre);
        Assert.Equal("b@example.com", bEnDb.Email);
    }
}
