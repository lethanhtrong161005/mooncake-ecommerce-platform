namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Development and staging OTP transport until an SMS provider is configured.</summary>
public class ConsoleRegisterOtpSender(IHostEnvironment environment, ILogger<ConsoleRegisterOtpSender> logger) : IRegisterOtpSender
{
    public bool IsAvailable => environment.IsDevelopment() || environment.IsStaging();

    public Task SendAsync(string phone, string otp, CancellationToken cancellationToken)
    {
        if (!IsAvailable) throw new InvalidOperationException("SMS provider is not configured.");
        logger.LogInformation("[SMS CONSOLE] Mã OTP đăng ký cho {Phone}: {Otp} (hết hạn sau 5 phút)", phone, otp);
        return Task.CompletedTask;
    }
}
