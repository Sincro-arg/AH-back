using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AH.Tests;

// Verificacion de punta a punta contra el despliegue REAL (Render + Supabase),
// no contra los controllers en memoria como el resto de la suite. Pega por red
// a la URL publica del back, la misma que usa el front en produccion
// (AH-front/src/environments/environment.ts). Cubre el punto 4 del pliego:
// registro, login, admin sembrado, ver/editar perfil, cambiar password, cambiar
// tema y volver a entrar con la password nueva.
//
// TODAVIA NO SE PUDO CORRER: este repo no tiene un .sln, y AH.Api.csproj vive
// en la raiz junto con la carpeta AH.Tests. Un "dotnet test" sin argumentos
// desde la raiz (que es como lo dispara la tarea "test") toma directamente
// AH.Api.csproj -que no tiene tests- y termina en éxito trivial sin haber
// corrido ni un solo test de este proyecto ni del resto de AH.Tests. Agregar
// un .sln en la raiz tampoco alcanza: ahi "dotnet test" pasa a fallar por
// ambiguedad (mas de un .csproj/.sln en el mismo directorio). Para poder
// correr esto (y el resto de AH.Tests) hace falta reordenar el proyecto -por
// ejemplo, mover AH.Api a su propia carpeta como ya esta AH.Tests- lo cual
// puede afectar el build/start command configurado en Render y no correspondia
// decidirlo en esta tarea. Quede con Skip para no crear usuarios de prueba en
// la base de produccion en cada corrida una vez que se resuelva el wiring.
public class RealDeploymentE2ETests
{
    private const string BaseUrl = "https://ah-back.onrender.com/api";
    private const string SkipReason = "No se pudo correr: ver comentario de la clase (falta wiring de .sln en AH-back).";

    private static HttpClient CrearCliente() => new() { BaseAddress = new Uri(BaseUrl) };

    private static async Task<JsonElement> Body(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>());

    [Fact(Skip = SkipReason)]
    public async Task FlujoCompleto_RegistroLoginPerfilPasswordTemaYAdminSembrado()
    {
        using var http = CrearCliente();
        var email = $"e2e-{Guid.NewGuid():N}@example.com";
        const string passwordInicial = "PasswordInicial1";
        const string passwordNueva = "PasswordNueva1";

        // 1) Registro
        var registerRes = await http.PostAsJsonAsync("auth/register", new
        {
            nombre = "E2E",
            apellido = "Test",
            email,
            telefono = "1122334455",
            password = passwordInicial,
        });
        Assert.Equal(HttpStatusCode.OK, registerRes.StatusCode);
        var registerBody = await Body(registerRes);
        Assert.False(string.IsNullOrWhiteSpace(registerBody.GetProperty("mensaje").GetString()));

        // 2) Login con el usuario recien creado
        var loginRes = await http.PostAsJsonAsync("auth/login", new { email, password = passwordInicial });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginBody = await Body(loginRes);
        var token = loginBody.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        var usuarioLogin = loginBody.GetProperty("usuario");
        Assert.Equal(email, usuarioLogin.GetProperty("email").GetString());
        Assert.Equal("claro", usuarioLogin.GetProperty("tema").GetString());
        var id = usuarioLogin.GetProperty("id").GetString();

        // 3) GET /usuarios/me
        http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var meRes = await http.GetAsync("usuarios/me");
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        var meBody = await Body(meRes);
        Assert.Equal(id, meBody.GetProperty("id").GetString());
        Assert.Equal(email, meBody.GetProperty("email").GetString());

        // 4) PUT /usuarios/me (editar perfil)
        var nuevoEmail = $"e2e-editado-{Guid.NewGuid():N}@example.com";
        var updateRes = await http.PutAsJsonAsync("usuarios/me", new
        {
            nombre = "E2E Editado",
            apellido = "Test Editado",
            telefono = "5599887766",
            email = nuevoEmail,
        });
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var updateBody = await Body(updateRes);
        Assert.Equal("E2E Editado", updateBody.GetProperty("nombre").GetString());
        Assert.Equal(nuevoEmail, updateBody.GetProperty("email").GetString());

        // 5) PUT /usuarios/me/password (cambiar password)
        var passRes = await http.PutAsJsonAsync("usuarios/me/password", new
        {
            passwordActual = passwordInicial,
            passwordNueva,
        });
        Assert.Equal(HttpStatusCode.OK, passRes.StatusCode);

        // 6) "logout y volver a entrar": tirar el token viejo y loguearse de nuevo
        //    con el email nuevo y la password nueva.
        http.DefaultRequestHeaders.Authorization = null;
        var reloginRes = await http.PostAsJsonAsync("auth/login", new { email = nuevoEmail, password = passwordNueva });
        Assert.Equal(HttpStatusCode.OK, reloginRes.StatusCode);
        var reloginBody = await Body(reloginRes);
        var token2 = reloginBody.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token2));

        // 7) PUT /usuarios/me/tema
        http.DefaultRequestHeaders.Authorization = new("Bearer", token2);
        var temaRes = await http.PutAsJsonAsync("usuarios/me/tema", new { tema = "oscuro" });
        Assert.Equal(HttpStatusCode.OK, temaRes.StatusCode);
        var temaBody = await Body(temaRes);
        Assert.Equal("oscuro", temaBody.GetProperty("tema").GetString());

        // 8) Login con el usuario de ejemplo sembrado por DbSeeder
        using var httpAdmin = CrearCliente();
        var adminLoginRes = await httpAdmin.PostAsJsonAsync("auth/login", new
        {
            email = "admin@cuentas.com",
            password = "Admin123!",
        });
        Assert.Equal(HttpStatusCode.OK, adminLoginRes.StatusCode);
        var adminLoginBody = await Body(adminLoginRes);
        var adminToken = adminLoginBody.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(adminToken));
        Assert.Equal("admin@cuentas.com", adminLoginBody.GetProperty("usuario").GetProperty("email").GetString());

        // 9) GET /usuarios/me con el admin
        httpAdmin.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        var adminMeRes = await httpAdmin.GetAsync("usuarios/me");
        Assert.Equal(HttpStatusCode.OK, adminMeRes.StatusCode);
        var adminMeBody = await Body(adminMeRes);
        Assert.Equal("admin@cuentas.com", adminMeBody.GetProperty("email").GetString());
    }
}
