using DeudoresApi.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DeudoresApi.Infrastructure.Events;

/// <summary>
/// Implementación de IEventPublisher que escribe en el log estructurado.
/// Para conectar SQS, RabbitMQ u otro broker, se crea una nueva clase
/// que implemente IEventPublisher y se registra en Program.cs — sin tocar Application.
/// </summary>
public class LogEventPublisher(ILogger<LogEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(T @event) where T : class
    {
        logger.LogInformation("Domain event: {EventType} {@Event}", typeof(T).Name, @event);
        return Task.CompletedTask;
    }
}
