namespace DeudoresApi.Domain.Events;

/// <summary>
/// Evento de dominio emitido al finalizar el procesamiento de un archivo.
/// Al ser un record inmutable, sirve como mensaje confiable para cualquier sistema
/// de mensajería (SQS, RabbitMQ, etc.) sin cambiar el código de Application.
/// </summary>
public record ImportCompletedEvent(
    int DeudoresCount,
    int EntidadesCount,
    DateTime ProcessedAt);
