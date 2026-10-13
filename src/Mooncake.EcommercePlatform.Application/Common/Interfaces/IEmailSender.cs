namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Delivers transactional email messages.</summary>
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
