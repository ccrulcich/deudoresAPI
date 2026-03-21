using DeudoresApi.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DeudoresApi.Infrastructure.Events;

/// <summary>
/// Implementación de IEventPublisher que escribe en el log estructurado.
/// Cumple el requerimiento de notificación del challenge.
///
/// Para conectar SQS, RabbitMQ, webhook u otro broker:
///   1. Crear una nueva clase: SqsEventPublisher : IEventPublisher
///   2. Cambiar el registro en Program.cs: AddScoped&lt;IEventPublisher, SqsEventPublisher&gt;
///   3. ImportService no se toca — no sabe qué implementación se usa.
/// </summary>
public class LogEventPublisher(ILogger<LogEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(T @event) where T : class
    {
        // {@Event} es la sintaxis de Serilog para serializar el objeto completo como JSON estructurado.
        // En un sink como Seq o Elasticsearch, esto permite filtrar y buscar por campos del evento.
        logger.LogInformation(
            "NOTIFICACIÓN — {EventType}: {@Event}",
            typeof(T).Name,
            @event);

        return Task.CompletedTask;
    }
}
