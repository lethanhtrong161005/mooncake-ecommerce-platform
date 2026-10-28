namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Sends email using configured SMTP settings with legacy environment-key overrides.</summary>
public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private const string MissingSettings = "SMTP host and sender must be configured using SMTP_HOST/SMTP_FROM_EMAIL or Smtp:Host/Smtp:FromEmail.";

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = configuration["SMTP_HOST"] ?? configuration["Smtp:Host"];
        var sender = configuration["SMTP_FROM_EMAIL"] ?? configuration["Smtp:FromEmail"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
            throw new InvalidOperationException(MissingSettings);

        var portText = configuration["SMTP_PORT"] ?? configuration["Smtp:Port"];
        var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !string.Equals(configuration["SMTP_USE_SSL"] ?? configuration["Smtp:UseSsl"], "false", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        var username = configuration["SMTP_USERNAME"] ?? configuration["Smtp:Username"];
        var password = configuration["SMTP_PASSWORD"] ?? configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage(sender, recipient, subject, body) { IsBodyHtml = false };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
