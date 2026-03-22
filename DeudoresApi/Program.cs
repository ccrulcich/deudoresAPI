using Amazon.SQS;
using DeudoresApi.Application.Services;
using DeudoresApi.Domain.Events;
using DeudoresApi.Domain.Messaging;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Domain.Services;
using DeudoresApi.Infrastructure.Data;
using DeudoresApi.Infrastructure.Events;
using DeudoresApi.Infrastructure.NameLookup;
using DeudoresApi.Infrastructure.Messaging;
using DeudoresApi.Infrastructure.Parsing;
using DeudoresApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net.Mime;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Iniciando DeudoresAPI...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Infrastructure — EF Core + PostgreSQL
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Infrastructure — Repositorios
    builder.Services.AddScoped<IDeudorRepository, DeudorRepository>();
    builder.Services.AddScoped<IEntidadRepository, EntidadRepository>();

    // Infrastructure — Parser
    builder.Services.AddScoped<IBcraParser, BcraParser>();

    // Infrastructure — Lookup de nombres (Nomdeu.txt / Maeent.txt)
    builder.Services.AddSingleton<INameLookupService, NameLookupService>();

    // Infrastructure — Event publishers (CompositeEventPublisher delega a todos en paralelo)
    builder.Services.AddScoped<LogEventPublisher>();
    builder.Services.AddScoped<WebhookEventPublisher>();
    builder.Services.AddScoped<EmailEventPublisher>();
    builder.Services.AddScoped<IEventPublisher, CompositeEventPublisher>(sp =>
        new CompositeEventPublisher([
            sp.GetRequiredService<LogEventPublisher>(),
            sp.GetRequiredService<WebhookEventPublisher>(),
            sp.GetRequiredService<EmailEventPublisher>()
        ]));

    builder.Services.AddHttpClient("webhook");

    // Application — Casos de uso
    builder.Services.AddScoped<IImportService, ImportService>();
    builder.Services.AddScoped<IQueryService, QueryService>();

    // SQS / procesamiento asíncrono (opcional)
    var sqsSettings = builder.Configuration.GetSection("SqsSettings").Get<SqsSettings>();
    if (!string.IsNullOrWhiteSpace(sqsSettings?.QueueUrl))
    {
        builder.Services.Configure<SqsSettings>(builder.Configuration.GetSection("SqsSettings"));

        builder.Services.AddSingleton<IAmazonSQS>(_ =>
        {
            var config = new AmazonSQSConfig { ServiceURL = sqsSettings.ServiceUrl };
            return new AmazonSQSClient(sqsSettings.AccessKey, sqsSettings.SecretKey, config);
        });

        builder.Services.AddScoped<IImportQueue, SqsImportQueue>();
        builder.Services.AddHostedService<SqsImportWorker>();

        Log.Information("SQS habilitado — cola: {QueueUrl}", sqsSettings.QueueUrl);
    }

    // Health checks — permite a Docker/K8s verificar que la API y la DB están saludables
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "postgresql",
            tags: ["db", "ready"]);

    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddControllers();

    var app = builder.Build();

    // Migraciones automáticas al iniciar (esencial para Docker: DB arranca vacía)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Log.Information("Migraciones aplicadas correctamente");
    }

    // Middleware global de excepciones — retorna error estructurado en lugar de 500 genérico
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = MediaTypeNames.Application.Json;

            var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (exceptionFeature is not null)
            {
                Log.Error(exceptionFeature.Error, "Error no controlado en {Path}", context.Request.Path);

                var error = new
                {
                    status = 500,
                    message = "Ocurrió un error interno en el servidor.",
                    traceId = context.TraceIdentifier
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            }
        });
    });

    app.UseSerilogRequestLogging();

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
}
finally
{
    Log.CloseAndFlush();
}

