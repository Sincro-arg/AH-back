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

    private static Usuario SeedUsuarioConPassword(AppDbContext db, string email, string password)
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

    [Fact]
    public async Task UpdatePassword_ConPasswordActualCorrecta_ActualizaElHash()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuarioConPassword(db, "a@example.com", "claveVieja1");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new UsuariosController.UpdatePasswordDto("claveVieja1", "claveNueva1");
        var res = await ctrl.UpdatePassword(dto);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal("Contraseña actualizada", GetProp(ok.Value!, "mensaje"));

        var enDb = await db.Usuarios.FindAsync(usuario.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("claveNueva1", enDb!.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("claveVieja1", enDb.PasswordHash));
    }

    [Fact]
    public async Task UpdatePassword_ConPasswordActualIncorrecta_Devuelve400()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuarioConPassword(db, "a@example.com", "claveVieja1");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new UsuariosController.UpdatePasswordDto("claveEquivocada", "claveNueva1");
        var res = await ctrl.UpdatePassword(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal("La contraseña actual no es correcta", GetProp(badRequest.Value!, "error"));

        var enDb = await db.Usuarios.FindAsync(usuario.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("claveVieja1", enDb!.PasswordHash));
    }

    [Fact]
    public async Task UpdatePassword_ConNuevaPasswordCorta_Devuelve400()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuarioConPassword(db, "a@example.com", "claveVieja1");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new UsuariosController.UpdatePasswordDto("claveVieja1", "corta1");
        var res = await ctrl.UpdatePassword(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal("La nueva contraseña debe tener al menos 8 caracteres", GetProp(badRequest.Value!, "error"));

        var enDb = await db.Usuarios.FindAsync(usuario.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("claveVieja1", enDb!.PasswordHash));
    }

    [Fact]
    public async Task UpdateTema_ConValorValido_ActualizaYDevuelveElTema()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db, "a@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new UsuariosController.UpdateTemaDto("oscuro");
        var res = await ctrl.UpdateTema(dto);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal("oscuro", GetProp(ok.Value!, "tema"));

        var enDb = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Equal("oscuro", enDb!.Tema);
    }

    [Fact]
    public async Task UpdateTema_ConValorInvalido_Devuelve400()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db, "a@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var dto = new UsuariosController.UpdateTemaDto("azul");
        var res = await ctrl.UpdateTema(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal("El tema debe ser 'claro' u 'oscuro'", GetProp(badRequest.Value!, "error"));

        var enDb = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Equal("claro", enDb!.Tema);
    }

    [Fact]
    public async Task UpdateTema_SinToken_Devuelve401()
    {
        var (ctrl, _) = Build();
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        var res = await ctrl.UpdateTema(new UsuariosController.UpdateTemaDto("oscuro"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(res);
        Assert.Equal("Token inválido", GetProp(unauthorized.Value!, "error"));
    }

    [Fact]
    public async Task DeleteMe_ConToken_BorraElUsuarioYDevuelve204()
    {
        var (ctrl, db) = Build();
        var usuario = SeedUsuario(db, "a@example.com");
        AutenticarComo(ctrl, usuario.Id);

        var res = await ctrl.DeleteMe();

        Assert.IsType<NoContentResult>(res);
        var enDb = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Null(enDb);
    }

    [Fact]
    public async Task DeleteMe_SoloBorraElUsuarioAutenticado_NoAOtros()
    {
        var (ctrl, db) = Build();
        var usuarioA = SeedUsuario(db, "a@example.com");
        var usuarioB = SeedUsuario(db, "b@example.com");
        AutenticarComo(ctrl, usuarioA.Id);

        await ctrl.DeleteMe();

        var bEnDb = await db.Usuarios.FindAsync(usuarioB.Id);
        Assert.NotNull(bEnDb);
    }

    [Fact]
    public async Task DeleteMe_SinToken_Devuelve401()
    {
        var (ctrl, _) = Build();
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        var res = await ctrl.DeleteMe();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(res);
        Assert.Equal("Token inválido", GetProp(unauthorized.Value!, "error"));
    }
}
