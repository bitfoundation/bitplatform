namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

public interface ISmsOtpReceiver
{
    bool IsSupported { get; }

    Task<string?> ReceiveOtp(CancellationToken cancellationToken);
}
