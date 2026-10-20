namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface IOtpRepository
{
    Task IssueAsync(string phone, string otpHash, DateTime nowUtc, bool resend, CancellationToken cancellationToken);
    Task<int> VerifyAsync(string phone, string otpHash, string tokenHash, DateTime nowUtc, CancellationToken cancellationToken);
    Task<User> RegisterAsync(string phone, string tokenHash, string fullName, string passwordHash, DateTime nowUtc, CancellationToken cancellationToken);
}
