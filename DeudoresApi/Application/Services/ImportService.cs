using DeudoresApi.Application.DTOs;
using DeudoresApi.Domain.Events;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Domain.Services;
using Microsoft.Extensions.Logging;

namespace DeudoresApi.Application.Services;

/// <summary>
/// Caso de uso: procesar un archivo BCRA completo.
/// Orquesta el parser, los repositorios y la publicación del evento.
/// No conoce EF Core, ni cómo se persiste, ni cómo se publica el evento.
/// </summary>
public class ImportService(
    IBcraParser parser,
    IDeudorRepository deudorRepo,
    IEntidadRepository entidadRepo,
    IEventPublisher eventPublisher,
    ILogger<ImportService> logger) : IImportService
{
    public async Task<ImportResultDto> ProcessAsync(Stream fileStream, CancellationToken ct = default)
    {
        logger.LogInformation("Iniciando procesamiento de archivo BCRA...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var (deudores, entidades) = await parser.ProcessAsync(fileStream, ct);

        logger.LogInformation(
            "Parsing completado: {DeudoresCount} deudores únicos, {EntidadesCount} entidades únicas",
            deudores.Count, entidades.Count);

        await deudorRepo.UpsertRangeAsync(deudores, ct);
        await entidadRepo.UpsertRangeAsync(entidades, ct);

        stopwatch.Stop();
        logger.LogInformation(
            "Persistencia completada en {ElapsedMs}ms",
            stopwatch.ElapsedMilliseconds);

        await eventPublisher.PublishAsync(new ImportCompletedEvent(
            deudores.Count,
            entidades.Count,
            DateTime.UtcNow));

        return new ImportResultDto(deudores.Count, entidades.Count);
    }
}
