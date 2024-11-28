using PhoneNumbers;
using Boilerplate.Server.Api;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Tests.Services;

public partial class FakePhoneService(ServerApiSettings appSettings, IHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor, ILogger<PhoneService> logger, PhoneNumberUtil phoneNumberUtil, RootServiceScopeProvider rootServiceScopeProvider)
    : PhoneService(appSettings, hostEnvironment, httpContextAccessor, logger, phoneNumberUtil, rootServiceScopeProvider)
{
    private static readonly ConcurrentDictionary<string, string> LastSmsPerPhone = new();

    public override Task SendSms(string messageText, string phoneNumber, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageText);
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);

        LastSmsPerPhone.AddOrUpdate(phoneNumber, messageText, (_, _) => messageText);
        return Task.CompletedTask;
    }

    public static string GetLastSmsFor(string phoneNumber, string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        if (LastSmsPerPhone.TryGetValue(phoneNumber, out var message) is false)
            Assert.IsNotNull(message, "Sms has not sent");

        if (pattern is not null && Regex.IsMatch(message, pattern) is false)
        {
            throw new AssertFailedException($"""
            Sms text does not match.
            expected pattern: {pattern}
            actual text: {message}
            """);
        }

        return message;
    }

    /// <summary>
    /// Extracts the last OTP sent to the specified phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number to check</param>
    /// <returns>The 6-digit OTP from the last SMS</returns>
    /// <exception cref="AssertException">Thrown when no valid OTP was found in the message</exception>
    public static string GetLastOtpFor(string phoneNumber, string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var message = GetLastSmsFor(phoneNumber, pattern);
        var otp = Regex.Match(message, @"\b\d{6}\b").Value;
        Assert.IsNotNull(otp, $"No valid 6-digit OTP found in message: {message}");
        return otp;
    }

    public static void Remove(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);
        LastSmsPerPhone.TryRemove(phoneNumber, out _);
    }

    public static void Clear()
    {
        LastSmsPerPhone.Clear();
    }
}
