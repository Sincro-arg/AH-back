using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AH.Tests;

// Verifica los OnChallenge/OnForbidden del JwtBearer registrados en Program.cs.
// El resto de la suite testea los controllers directo (sin pasar por el pipeline
// HTTP), asi que ese camino nunca ejercita lo que hace AddAuthentication cuando
// rechaza un request ANTES de llegar a ningun controller: sin esos eventos, ASP.NET
// Core devuelve 401 con el body vacio, distinto del resto de la API que siempre
// responde { error }. Por eso hace falta levantar la app entera con
// WebApplicationFactory<Program> y pegarle por HTTP real, igual que
// ExceptionHandlerMiddlewareTests.
public class JwtBearerEventsTests
{
    [Fact]
    public async Task RequestSinAuthorizationHeader_A_EndpointProtegido_Devuelve401ConCampoError()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/usuarios/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("error").GetString()));
    }

    [Fact]
    public async Task RequestConTokenInvalido_A_EndpointProtegido_Devuelve401ConCampoError()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer token-que-no-es-un-jwt-valido");

        var response = await client.GetAsync("/api/usuarios/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("error").GetString()));
    }
}
