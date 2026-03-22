using Amazon.SQS;
using Amazon.SQS.Model;
using DeudoresApi.Application.Services;
using Microsoft.Extensions.Options;

namespace DeudoresApi.Infrastructure.Messaging;

/// <summary>
/// BackgroundService que escucha la cola SQS y procesa archivos de forma asíncrona.
/// Corre en background desde que levanta la app, independiente de los requests HTTP.
/// </summary>
public class SqsImportWorker(
    IServiceScopeFactory scopeFactory,
    IAmazonSQS sqs,
    IOptions<SqsSettings> options,
    ILogger<SqsImportWorker> logger) : BackgroundService
{
    private readonly string _queueUrl = options.Value.QueueUrl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SqsImportWorker iniciado, escuchando cola: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 10  // long polling: reduce llamadas vacías a SQS
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown normal — salimos del loop
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al recibir mensajes de SQS, reintentando en 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("SqsImportWorker detenido");
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        var filePath = message.Body;
        logger.LogInformation("Worker procesando archivo: {FilePath}", filePath);

        try
        {
            // Cada mensaje se procesa en su propio scope de DI (DbContext, repositorios, etc.)
            using var scope = scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            await using var stream = File.OpenRead(filePath);
            var result = await importService.ProcessAsync(stream);

            logger.LogInformation(
                "Worker completó: {Deudores} deudores, {Entidades} entidades — archivo: {FilePath}",
                result.DeudoresCount, result.EntidadesCount, filePath);

            // Eliminar el archivo temporal luego de procesarlo con éxito
            // (solo archivos temporales — los montados como volumen se ignoran)
            try
            {
                if (File.Exists(filePath) && filePath.StartsWith(Path.GetTempPath()))
                    File.Delete(filePath);
            }
            catch (IOException)
            {
                logger.LogDebug("No se pudo eliminar {FilePath} (puede ser read-only o volumen montado)", filePath);
            }

            // Borrar el mensaje de la cola para que no sea reprocesado
            await sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando archivo {FilePath} — el mensaje vuelve a la cola", filePath);
            // No borramos el mensaje: SQS lo reintentará automáticamente al vencer el visibility timeout
        }
    }
}
