using AH.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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

builder.Services.AddControllers();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFront"); // antes de auth/routing para que las respuestas de error también lleven los headers
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
