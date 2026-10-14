namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Net;
using System.Net.Mail;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Sends email through SMTP settings supplied by environment variables.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST");
        var sender = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL");
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
            throw new InvalidOperationException("SMTP_HOST and SMTP_FROM_EMAIL must be configured to send email.");

        var portText = Environment.GetEnvironmentVariable("SMTP_PORT");
        var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !string.Equals(Environment.GetEnvironmentVariable("SMTP_USE_SSL"), "false", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage(sender, recipient, subject, body) { IsBodyHtml = false };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
