using System.Linq;
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
// AH.Api ahora vive en su propia carpeta (AH.Api/) y hay un AH.sln en la raiz
// que referencia AH.Api y AH.Tests, asi que "dotnet test" desde la raiz ya
// encuentra y corre este proyecto sin ambiguedad.
public class RealDeploymentE2ETests
{
    private const string BaseUrl = "https://ah-back.onrender.com/api/";

    private static HttpClient CrearCliente() => new() { BaseAddress = new Uri(BaseUrl) };

    private static async Task<JsonElement> Body(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>());

    [Fact]
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

    // Tarea 656: Pozos e Inversiones son nuevos y el resto de la suite los
    // prueba solo contra la base InMemory. Esto confirma que las migraciones
    // y los tipos (decimal, Guid, DateTime) tambien funcionan contra el
    // Postgres real de Supabase: crear pozo, invertir, pasar por Abierto ->
    // Comprado -> Vendido, y el reparto de ganancia final.
    [Fact]
    public async Task FlujoCompleto_PozosEInversiones()
    {
        using var httpAdmin = CrearCliente();
        var adminLoginRes = await httpAdmin.PostAsJsonAsync("auth/login", new
        {
            email = "admin@cuentas.com",
            password = "Admin123!",
        });
        Assert.Equal(HttpStatusCode.OK, adminLoginRes.StatusCode);
        var adminLoginBody = await Body(adminLoginRes);
        var adminToken = adminLoginBody.GetProperty("token").GetString();
        var adminId = adminLoginBody.GetProperty("usuario").GetProperty("id").GetString();
        httpAdmin.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        // 1) GET /pozos publico, sin token
        using var httpPublico = CrearCliente();
        var listaPublicaRes = await httpPublico.GetAsync("pozos");
        Assert.Equal(HttpStatusCode.OK, listaPublicaRes.StatusCode);
        var listaPublicaBody = await Body(listaPublicaRes);
        Assert.Equal(JsonValueKind.Array, listaPublicaBody.ValueKind);

        // 2) POST /pozos
        var titulo = $"Pozo E2E {Guid.NewGuid():N}";
        var crearRes = await httpAdmin.PostAsJsonAsync("pozos", new
        {
            titulo,
            autoDescripcion = "Auto de prueba E2E, motor 1.6, 80.000km",
            montoObjetivo = 500000.50m,
        });
        Assert.Equal(HttpStatusCode.Created, crearRes.StatusCode);
        var pozoBody = await Body(crearRes);
        var pozoId = pozoBody.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(pozoId));
        Assert.Equal("Abierto", pozoBody.GetProperty("estado").GetString());
        Assert.Equal(0, pozoBody.GetProperty("montoRecaudado").GetDecimal());
        Assert.Equal(500000.50m, pozoBody.GetProperty("montoObjetivo").GetDecimal());

        // 3) aparece en el listado publico
        var listaConPozoRes = await httpPublico.GetAsync("pozos");
        var listaConPozoBody = await Body(listaConPozoRes);
        Assert.Contains(listaConPozoBody.EnumerateArray(), p => p.GetProperty("id").GetString() == pozoId);

        // 4) GET /pozos/{id} (autenticado), sin inversiones todavia
        var detalleRes = await httpAdmin.GetAsync($"pozos/{pozoId}");
        Assert.Equal(HttpStatusCode.OK, detalleRes.StatusCode);
        var detalleBody = await Body(detalleRes);
        Assert.Empty(detalleBody.GetProperty("inversiones").EnumerateArray());

        // 5) POST /pozos/{id}/inversiones con monto invalido -> 400
        var invInvalidaRes = await httpAdmin.PostAsJsonAsync($"pozos/{pozoId}/inversiones", new { monto = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, invInvalidaRes.StatusCode);

        // 6) POST /pozos/{id}/inversiones valida
        var montoInvertido = 250000.25m;
        var invRes = await httpAdmin.PostAsJsonAsync($"pozos/{pozoId}/inversiones", new { monto = montoInvertido });
        Assert.Equal(HttpStatusCode.Created, invRes.StatusCode);
        var invBody = await Body(invRes);
        var inversionId = invBody.GetProperty("id").GetString();
        Assert.Equal(pozoId, invBody.GetProperty("pozoId").GetString());
        Assert.Equal(montoInvertido, invBody.GetProperty("monto").GetDecimal());

        // 7) GET /pozos/{id}/inversiones incluye la que se acaba de crear
        var listaInvRes = await httpAdmin.GetAsync($"pozos/{pozoId}/inversiones");
        Assert.Equal(HttpStatusCode.OK, listaInvRes.StatusCode);
        var listaInvBody = await Body(listaInvRes);
        Assert.Contains(listaInvBody.EnumerateArray(), i => i.GetProperty("id").GetString() == inversionId);

        // 8) montoRecaudado del pozo se actualizo
        var detalle2Res = await httpAdmin.GetAsync($"pozos/{pozoId}");
        var detalle2Body = await Body(detalle2Res);
        Assert.Equal(montoInvertido, detalle2Body.GetProperty("montoRecaudado").GetDecimal());

        // 9) un segundo usuario no puede tocar la inversion ajena del admin
        using var httpOtro = CrearCliente();
        var otroEmail = $"e2e-pozos-{Guid.NewGuid():N}@example.com";
        await httpOtro.PostAsJsonAsync("auth/register", new
        {
            nombre = "Otro",
            apellido = "Inversor",
            email = otroEmail,
            telefono = "1133445566",
            password = "PasswordOtro1",
        });
        var otroLoginRes = await httpOtro.PostAsJsonAsync("auth/login", new { email = otroEmail, password = "PasswordOtro1" });
        var otroToken = (await Body(otroLoginRes)).GetProperty("token").GetString();
        httpOtro.DefaultRequestHeaders.Authorization = new("Bearer", otroToken);

        var editarAjenaRes = await httpOtro.PutAsJsonAsync($"inversiones/{inversionId}", new { monto = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, editarAjenaRes.StatusCode);
        var borrarAjenaRes = await httpOtro.DeleteAsync($"inversiones/{inversionId}");
        Assert.Equal(HttpStatusCode.Forbidden, borrarAjenaRes.StatusCode);

        // 10) marcarComprado sin precioCompra -> 400
        var compradoInvalidoRes = await httpAdmin.PutAsJsonAsync($"pozos/{pozoId}/estado", new { accion = "marcarComprado" });
        Assert.Equal(HttpStatusCode.BadRequest, compradoInvalidoRes.StatusCode);

        // 11) marcarComprado valido
        var precioCompra = 480000.75m;
        var fechaCompra = DateTime.UtcNow.AddDays(-5);
        var compradoRes = await httpAdmin.PutAsJsonAsync($"pozos/{pozoId}/estado", new
        {
            accion = "marcarComprado",
            precioCompra,
            fechaCompra,
        });
        Assert.Equal(HttpStatusCode.OK, compradoRes.StatusCode);
        var compradoBody = await Body(compradoRes);
        Assert.Equal("Comprado", compradoBody.GetProperty("estado").GetString());
        Assert.Equal(precioCompra, compradoBody.GetProperty("precioCompra").GetDecimal());

        // 12) marcarComprado de nuevo -> 409 (ya no esta Abierto)
        var compradoConflictoRes = await httpAdmin.PutAsJsonAsync($"pozos/{pozoId}/estado", new
        {
            accion = "marcarComprado",
            precioCompra,
            fechaCompra,
        });
        Assert.Equal(HttpStatusCode.Conflict, compradoConflictoRes.StatusCode);

        // 13) ya no se pueden cargar inversiones en un pozo Comprado
        var invSobrePozoCompradoRes = await httpAdmin.PostAsJsonAsync($"pozos/{pozoId}/inversiones", new { monto = 100 });
        Assert.Equal(HttpStatusCode.Conflict, invSobrePozoCompradoRes.StatusCode);

        // 14) marcarVendido valido
        var precioVenta = 600000.10m;
        var fechaVenta = DateTime.UtcNow;
        var vendidoRes = await httpAdmin.PutAsJsonAsync($"pozos/{pozoId}/estado", new
        {
            accion = "marcarVendido",
            precioVenta,
            fechaVenta,
        });
        Assert.Equal(HttpStatusCode.OK, vendidoRes.StatusCode);
        var vendidoBody = await Body(vendidoRes);
        Assert.Equal("Vendido", vendidoBody.GetProperty("estado").GetString());
        Assert.Equal(precioVenta, vendidoBody.GetProperty("precioVenta").GetDecimal());

        // 15) GET /pozos/{id}/reparto
        var repartoRes = await httpAdmin.GetAsync($"pozos/{pozoId}/reparto");
        Assert.Equal(HttpStatusCode.OK, repartoRes.StatusCode);
        var repartoBody = await Body(repartoRes);
        var gananciaTotal = repartoBody.GetProperty("gananciaTotal").GetDecimal();
        Assert.Equal(precioVenta - precioCompra, gananciaTotal);
        var reparto = repartoBody.GetProperty("reparto").EnumerateArray().ToList();
        var filaReparto = Assert.Single(reparto);
        Assert.Equal(adminId, filaReparto.GetProperty("usuarioId").GetString());
        Assert.Equal(montoInvertido, filaReparto.GetProperty("montoInvertido").GetDecimal());
        Assert.Equal(1m, filaReparto.GetProperty("porcentaje").GetDecimal());
        Assert.Equal(gananciaTotal, filaReparto.GetProperty("ganancia").GetDecimal());

        // 16) reparto de un pozo que todavia no fue vendido -> 409
        var otroPozoRes = await httpAdmin.PostAsJsonAsync("pozos", new
        {
            titulo = $"Pozo E2E sin vender {Guid.NewGuid():N}",
            autoDescripcion = "sin vender",
            montoObjetivo = 1000m,
        });
        var otroPozoId = (await Body(otroPozoRes)).GetProperty("id").GetString();
        var repartoNoVendidoRes = await httpAdmin.GetAsync($"pozos/{otroPozoId}/reparto");
        Assert.Equal(HttpStatusCode.Conflict, repartoNoVendidoRes.StatusCode);

        // 17) DELETE de ese pozo, Abierto y sin inversiones -> 204
        var eliminarOtroPozoRes = await httpAdmin.DeleteAsync($"pozos/{otroPozoId}");
        Assert.Equal(HttpStatusCode.NoContent, eliminarOtroPozoRes.StatusCode);

        // 18) GET /usuarios/me/inversiones refleja la ganancia del pozo ya vendido
        var misInversionesRes = await httpAdmin.GetAsync("usuarios/me/inversiones");
        Assert.Equal(HttpStatusCode.OK, misInversionesRes.StatusCode);
        var misInversionesBody = await Body(misInversionesRes);
        var miInversion = misInversionesBody.EnumerateArray()
            .First(i => i.GetProperty("id").GetString() == inversionId);
        Assert.Equal(pozoId, miInversion.GetProperty("pozoId").GetString());
        Assert.Equal("Vendido", miInversion.GetProperty("estadoPozo").GetString());
        Assert.Equal(gananciaTotal, miInversion.GetProperty("gananciaCorrespondiente").GetDecimal());

        // 19) una inversion en un pozo ya Vendido no se puede editar ni borrar
        var editarVendidaRes = await httpAdmin.PutAsJsonAsync($"inversiones/{inversionId}", new { monto = 1000 });
        Assert.Equal(HttpStatusCode.Conflict, editarVendidaRes.StatusCode);
        var borrarVendidaRes = await httpAdmin.DeleteAsync($"inversiones/{inversionId}");
        Assert.Equal(HttpStatusCode.Conflict, borrarVendidaRes.StatusCode);
    }
}
