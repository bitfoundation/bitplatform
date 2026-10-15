namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

/// <summary>
/// Reads the OTP out of an incoming SMS for the Android app only. Browsers keep filling it through their own WebOTP
/// API; Android's WebView has none, so Scripts/SmsOtp.ts hands BitOtpInput's WebOTP request to this instead.
/// </summary>
public interface ISmsOtpReceiver
{
    bool IsSupported { get; }

    Task<string?> ReceiveOtp(CancellationToken cancellationToken);
}
