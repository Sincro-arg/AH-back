using AH.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Puerto fijo (5203) para que coincida siempre con proxy.conf.json del front,
// sin depender de que se respete el launch profile. Si el entorno de ejecucion
// fija PORT (por ejemplo en un hosting como Render), se usa ese en su lugar.
var portEnv = Environment.GetEnvironmentVariable("PORT");
var port = string.IsNullOrEmpty(portEnv) ? "5203" : portEnv;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// La cadena viene de configuracion (appsettings) o de la variable de entorno
// ConnectionStrings__DefaultConnection en produccion. Se normaliza el pooling acá
// porque contra el pooler de Supabase (puerto 6543) cada conexion nueva reautentica:
// sin pool, cada request abre y autentica de cero.
var dbConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(dbConnStr))
{
    var csb = new Npgsql.NpgsqlConnectionStringBuilder(dbConnStr)
    {
        Pooling = true,
        MinPoolSize = 0,
        MaxPoolSize = 20,
        NoResetOnClose = true,
    };
    dbConnStr = csb.ConnectionString;
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dbConnStr));

// InvalidModelStateResponseFactory: los 400 automaticos de [ApiController] (JSON
// invalido, un Guid con formato invalido en la ruta, etc.) devuelven por defecto un
// ProblemDetails de ASP.NET Core. Se lo reemplaza aca para que el front reciba
// siempre el mismo shape de error { error } que el resto de la API.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var mensaje = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "La solicitud tiene datos inválidos.";
            return new BadRequestObjectResult(new { error = mensaje });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AH API", Version = "v1" });
});

// CORS: habilitado para el front en desarrollo. Se permite cualquier origen porque en
// este esqueleto todavia no hay credenciales/cookies en juego y el puerto del front
// puede variar segun donde se levante.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFront", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// La clave de firma viene de configuracion (appsettings) o de la variable de entorno
// Jwt__Key en produccion; nunca hardcodeada.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no está configurado. Definí la variable de entorno Jwt__Key.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFront"); // antes de auth/routing para que las respuestas de error también lleven los headers

// Exception handler global: cualquier excepcion no controlada que llegue hasta aca
// (una query de EF que falla, un null reference, lo que sea) devuelve siempre
// { error } con 500 en vez de tumbar la respuesta o exponer un stack trace. Va
// primero en la tuberia (despues de CORS, para que la respuesta de error tambien
// lleve esos headers) para que envuelva a todo lo que viene despues: auth, authz
// y los controllers.
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Ocurrió un error inesperado en el servidor. Intentá de nuevo más tarde." });
    });
});

// Sin UseHttpsRedirection: Render (y hostings similares) terminan el TLS en su
// proxy y reenvian HTTP puro al contenedor, que solo escucha http://0.0.0.0:{PORT}
// (ver arriba). Redirigir a https aca generaba un loop contra ese proxy.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Aplica migraciones pendientes y siembra el usuario de prueba del pliego
// (admin@cuentas.com). No hay un paso de deploy separado que corra
// `dotnet ef database update`, asi que se hace al arrancar. Se registra para
// correr recien despues de ApplicationStarted -es decir, con Kestrel ya escuchando
// el puerto- para que un problema o demora contra la base (cold start, DNS, etc.)
// nunca bloquee el bind del puerto: si esto corriera antes de app.Run(), un hosting
// con health check por puerto (como Render) nunca ve el servicio arriba y todo pedido
// externo cuelga hasta el timeout, aunque el proceso este vivo. Va en try/catch
// porque no tiene que tumbar el arranque si la base no esta disponible en ese momento.
app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        DbSeeder.SeedAdminUsuario(db);
        var pozoVwGol = DbSeeder.SeedPozos(db);
        DbSeeder.SeedInversiones(db, pozoVwGol);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "No se pudo migrar/sembrar la base de datos");
    }
});

app.Run();

// Se expone la clase Program (generada por los top-level statements) para que
// AH.Tests pueda usar WebApplicationFactory<Program> y testear el pipeline HTTP
// completo (exception handler global incluido).
public partial class Program { }
