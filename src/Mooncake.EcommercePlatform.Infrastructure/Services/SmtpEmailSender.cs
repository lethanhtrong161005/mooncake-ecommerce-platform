namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Sends transactional email using Smtp settings, with environment overrides.</summary>
public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? configuration["Smtp:Host"];
        var sender = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? configuration["Smtp:FromEmail"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
            throw new InvalidOperationException("Smtp:Host and Smtp:FromEmail must be configured to send email.");

        var portText = Environment.GetEnvironmentVariable("SMTP_PORT") ?? configuration["Smtp:Port"];
        var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !string.Equals(Environment.GetEnvironmentVariable("SMTP_USE_SSL") ?? configuration["Smtp:UseSsl"], "false", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? configuration["Smtp:Username"];
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? configuration["Smtp:Password"];
        username = string.IsNullOrWhiteSpace(username) ? sender : username.Trim();
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Smtp:Password must contain a Gmail App Password.");
        if (string.Equals(host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
            password = password.Replace(" ", string.Empty, StringComparison.Ordinal);
        client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage(sender, recipient, subject, body) { IsBodyHtml = false };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
