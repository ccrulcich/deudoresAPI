using DeudoresApi.Domain.Events;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace DeudoresApi.Infrastructure.Events;

/// <summary>
/// Publica eventos enviando un email via SMTP.
/// Si la configuración de email no está presente, omite el envío silenciosamente.
///
/// Configuración requerida en appsettings.json o variables de entorno:
///   Notifications:Email:SmtpHost
///   Notifications:Email:SmtpPort
///   Notifications:Email:Username
///   Notifications:Email:Password
///   Notifications:Email:From
///   Notifications:Email:To
///
/// Usa MailKit (no SmtpClient del framework, que está obsoleto desde .NET 5).
/// </summary>
public class EmailEventPublisher(
    IConfiguration configuration,
    ILogger<EmailEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync<T>(T @event) where T : class
    {
        var emailConfig = configuration.GetSection("Notifications:Email");
        var smtpHost = emailConfig["SmtpHost"];

        // Si no hay host SMTP configurado, omitimos sin error.
        // Permite que el sistema funcione en desarrollo sin cuenta de correo.
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            logger.LogDebug("Email SMTP no configurado (Notifications:Email:SmtpHost vacío), notificación omitida");
            return;
        }

        try
        {
            var port = int.TryParse(emailConfig["SmtpPort"], out var p) ? p : 587;
            var username = emailConfig["Username"] ?? string.Empty;
            var password = emailConfig["Password"] ?? string.Empty;
            var from = emailConfig["From"] ?? username;
            var to = emailConfig["To"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(to))
            {
                logger.LogWarning("Email omitido: Notifications:Email:To no está configurado");
                return;
            }

            var message = BuildMessage(from, to, @event);

            using var client = new SmtpClient();

            // SecureSocketOptions.Auto: detecta automáticamente si usar SSL/TLS o STARTTLS
            // según el puerto (465 → SSL, 587 → STARTTLS, 25 → sin cifrado)
            await client.ConnectAsync(smtpHost, port, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation(
                "Email de notificación enviado a {Recipient} vía {SmtpHost}:{Port}",
                to, smtpHost, port);
        }
        catch (Exception ex)
        {
            // El fallo de email no debe interrumpir el flujo principal —
            // el procesamiento ya fue exitoso.
            logger.LogError(ex, "Error al enviar email de notificación");
        }
    }

    private static MimeMessage BuildMessage<T>(string from, string to, T @event) where T : class
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = $"[DeudoresAPI] Procesamiento completado — {typeof(T).Name}";

        // Serializa el evento como JSON con indentación para que sea legible en el cuerpo del mail
        var jsonBody = System.Text.Json.JsonSerializer.Serialize(
            @event,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        message.Body = new TextPart("plain")
        {
            Text = $"El procesamiento finalizó exitosamente.\n\nDetalle del evento:\n\n{jsonBody}"
        };

        return message;
    }
}
