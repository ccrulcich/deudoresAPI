namespace DeudoresApi.Domain.Messaging;

/// <summary>
/// Contrato para encolar un trabajo de importación.
/// Definido en Domain para que Application no dependa de SQS directamente.
/// </summary>
public interface IImportQueue
{
    Task EnqueueAsync(string filePath, CancellationToken ct = default);
}
