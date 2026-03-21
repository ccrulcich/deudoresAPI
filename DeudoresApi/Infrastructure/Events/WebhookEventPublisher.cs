using DeudoresApi.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace DeudoresApi.Infrastructure.Events;

/// <summary>
/// Publica eventos como un HTTP POST a una URL configurable.
/// Si WEBHOOK_URL no está configurada, la notificación se omite silenciosamente.
///
/// Para activarlo, setear en appsettings.json o variable de entorno:
///   "Notifications:WebhookUrl": "https://tu-sistema.com/hooks/import"
///
/// El body del POST es el evento serializado como JSON:
///   { "deudoresCount": 1500, "entidadesCount": 30, "processedAt": "..." }
/// </summary>
public class WebhookEventPublisher(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<WebhookEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync<T>(T @event) where T : class
    {
        var webhookUrl = configuration["Notifications:WebhookUrl"];

        // Si no hay URL configurada, no hacemos nada.
        // Esto permite que la app funcione en desarrollo sin configurar el webhook.
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogDebug("Webhook no configurado (Notifications:WebhookUrl vacío), notificación omitida");
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("webhook");

            // PostAsJsonAsync serializa el evento como JSON y hace el POST.
            var response = await client.PostAsJsonAsync(webhookUrl, @event);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Webhook enviado a {WebhookUrl} — respuesta: {StatusCode}",
                    webhookUrl, (int)response.StatusCode);
            }
            else
            {
                logger.LogWarning(
                    "Webhook a {WebhookUrl} respondió con error: {StatusCode}",
                    webhookUrl, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // El fallo del webhook no debe interrumpir el flujo principal.
            // El procesamiento ya fue exitoso — solo la notificación falló.
            logger.LogError(ex,
                "Error al enviar webhook a {WebhookUrl}",
                webhookUrl);
        }
    }
}
