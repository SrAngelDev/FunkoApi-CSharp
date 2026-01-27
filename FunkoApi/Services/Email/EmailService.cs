using FunkoApi.Configuration;

namespace FunkoApi.Services.Email;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public class EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        try
        {
            var message = new MimeMessage();
            
            // Remitente
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            // Destinatario
            message.To.Add(new MailboxAddress("", to));
            // Asunto
            message.Subject = subject;

            // Cuerpo del mensaje (HTML)
            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            // Cliente SMTP
            using var client = new SmtpClient();
            
            // Conexión segura
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            
            // Autenticación
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPass);
            
            // Envío
            await client.SendAsync(message);
            
            // Desconexión
            await client.DisconnectAsync(true);
            
            logger.LogInformation($"Email enviado correctamente a {to}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error enviando email a {to}: {ex.Message}");
        }
    }
}