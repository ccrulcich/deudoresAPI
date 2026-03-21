using DeudoresApi.Application.Services;
using DeudoresApi.Domain.Events;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Domain.Services;
using DeudoresApi.Infrastructure.Data;
using DeudoresApi.Infrastructure.Events;
using DeudoresApi.Infrastructure.Parsing;
using DeudoresApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Serilog se configura antes de crear el builder para capturar
// errores de startup (ej: connection string inválida) que ocurren
// antes de que el DI container esté listo.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext() // permite adjuntar propiedades contextuales (ej: RequestId)
    .CreateLogger();

try
{
    Log.Information("Iniciando DeudoresAPI...");

    var builder = WebApplication.CreateBuilder(args);

    // Reemplaza el logging provider de .NET por Serilog.
    // Esto redirige todos los ILogger<T> inyectados en el sistema a Serilog.
    builder.Host.UseSerilog();

    // Infrastructure — EF Core + PostgreSQL
    // La connection string se lee desde appsettings.json o variable de entorno
    // (Docker: ConnectionStrings__DefaultConnection=...)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Infrastructure — Repositorios concretos (Scoped: comparten DbContext por request)
    builder.Services.AddScoped<IDeudorRepository, DeudorRepository>();
    builder.Services.AddScoped<IEntidadRepository, EntidadRepository>();

    // Infrastructure — Parser concreto (Scoped)
    builder.Services.AddScoped<IBcraParser, BcraParser>();

    // Infrastructure — Event publishers
    // CompositeEventPublisher delega a todos los publishers registrados en paralelo.
    // Para agregar un nuevo canal (SQS, email, etc.) solo agregar un AddScoped más aquí.
    builder.Services.AddScoped<LogEventPublisher>();
    builder.Services.AddScoped<WebhookEventPublisher>();
    builder.Services.AddScoped<EmailEventPublisher>();
    builder.Services.AddScoped<IEventPublisher, CompositeEventPublisher>(sp =>
        new CompositeEventPublisher([
            sp.GetRequiredService<LogEventPublisher>(),
            sp.GetRequiredService<WebhookEventPublisher>(),
            sp.GetRequiredService<EmailEventPublisher>()
        ]));

    // IHttpClientFactory para el webhook — permite reuso de conexiones HTTP
    builder.Services.AddHttpClient("webhook");

    // Application — Casos de uso
    builder.Services.AddScoped<IImportService, ImportService>();
    builder.Services.AddScoped<IQueryService, QueryService>();

    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddControllers();

    var app = builder.Build();

    // Middleware de Serilog: loguea cada request HTTP con duración, status code, etc.
    // Produce logs como: HTTP POST /Import/upload responded 200 in 143ms
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.MapControllers();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.Run();
}
catch (Exception ex)
{
    // Captura fallos fatales en el startup (ej: no puede conectar a la DB)
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
}
finally
{
    // Garantiza que todos los logs pendientes en buffer se escriban antes de salir
    Log.CloseAndFlush();
}

