using DeudoresApi.Application.DTOs;
using DeudoresApi.Domain.Events;
using DeudoresApi.Domain.Repositories;
using DeudoresApi.Domain.Services;

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
    IEventPublisher eventPublisher) : IImportService
{
    public async Task<ImportResultDto> ProcessAsync(Stream fileStream)
    {
        var (deudores, entidades) = await parser.ProcessAsync(fileStream);

        await deudorRepo.UpsertRangeAsync(deudores);
        await entidadRepo.UpsertRangeAsync(entidades);

        // Publica el evento de dominio. Hoy logea; mañana puede enrutar a SQS/RabbitMQ
        // sin tocar este código — solo se swapea la implementación de IEventPublisher.
        await eventPublisher.PublishAsync(new ImportCompletedEvent(
            deudores.Count,
            entidades.Count,
            DateTime.UtcNow));

        return new ImportResultDto(deudores.Count, entidades.Count);
    }
}
