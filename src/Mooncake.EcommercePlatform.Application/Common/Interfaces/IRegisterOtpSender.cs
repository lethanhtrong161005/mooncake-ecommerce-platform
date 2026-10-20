namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

public interface IRegisterOtpSender
{
    bool IsAvailable { get; }
    Task SendAsync(string phone, string otp, CancellationToken cancellationToken);
}
