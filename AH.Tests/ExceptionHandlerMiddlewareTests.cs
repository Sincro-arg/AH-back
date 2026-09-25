using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AH.Tests;

// Verifica el exception handler global registrado en Program.cs. No alcanza con
// llamar al controller directamente (como hace el resto de la suite): el
// middleware vive en la tuberia HTTP, asi que hay que levantar la app entera con
// WebApplicationFactory<Program> y pegarle por HTTP de verdad.
//
// Para forzar una excepcion no controlada sin tocar ningun controller de
// produccion, se agrega -solo para este test- un endpoint que siempre tira una
// excepcion, via un IStartupFilter que se registra despues del pipeline real de
// Program.cs (por eso queda "envuelto" por app.UseExceptionHandler, igual que
// cualquier controller real).
public class ExceptionHandlerMiddlewareTests
{
    private class ThrowingEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                app.Map("/api/test-error", branch =>
                    branch.Run(_ => throw new InvalidOperationException("Fallo simulado para test del exception handler.")));
            };
        }
    }

    private static WebApplicationFactory<Program> CrearFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new ThrowingEndpointStartupFilter());
            });
        });

    [Fact]
    public async Task ExcepcionNoControlada_Devuelve500ConCampoErrorEntendible()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/test-error");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("error").GetString()));
    }
}
