using Amazon.SQS;
using Amazon.SQS.Model;
using DeudoresApi.Domain.Messaging;
using Microsoft.Extensions.Options;

namespace DeudoresApi.Infrastructure.Messaging;

/// <summary>
/// Publica mensajes en una cola SQS (o LocalStack en local).
/// El mensaje es simplemente la ruta del archivo a procesar.
/// </summary>
public class SqsImportQueue(IAmazonSQS sqs, IOptions<SqsSettings> options) : IImportQueue
{
    private readonly string _queueUrl = options.Value.QueueUrl;

    public async Task EnqueueAsync(string filePath, CancellationToken ct = default)
    {
        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = filePath
        }, ct);
    }
}
