using DeudoresApi.Domain.Events;

namespace DeudoresApi.Infrastructure.Events;

/// <summary>
/// Composite publisher: delega a múltiples IEventPublisher en paralelo.
///
/// Permite que el sistema notifique por varios canales simultáneamente
/// (log + webhook, o log + SQS + webhook) sin que ImportService lo sepa.
///
/// ImportService solo conoce IEventPublisher — este composite es transparente.
/// Para agregar un nuevo canal: registrar una implementación más en Program.cs.
/// </summary>
public class CompositeEventPublisher(IEnumerable<IEventPublisher> publishers) : IEventPublisher
{
    public async Task PublishAsync<T>(T @event) where T : class
    {
        // Ejecuta todos los publishers en paralelo para no bloquear secuencialmente
        await Task.WhenAll(publishers.Select(p => p.PublishAsync(@event)));
    }
}
