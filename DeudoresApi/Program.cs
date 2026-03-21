using DeudoresApi.Application.Services;
using DeudoresApi.Domain.Events;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Domain.Services;
using DeudoresApi.Infrastructure.Data;
using DeudoresApi.Infrastructure.Events;
using DeudoresApi.Infrastructure.Parsing;
using DeudoresApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// Infrastructure — Event publisher (hoy: log; swap por SQS sin tocar Application)
builder.Services.AddScoped<IEventPublisher, LogEventPublisher>();

// Application — Casos de uso
builder.Services.AddScoped<IImportService, ImportService>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

