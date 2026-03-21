namespace DeudoresApi.Domain.Events;

/// <summary>
/// Abstracción para publicar eventos de dominio.
/// La implementación concreta (log, SQS, webhook, etc.) vive en Infrastructure.
/// Application solo conoce esta interfaz — swap sin tocar lógica de negocio.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event) where T : class;
}
