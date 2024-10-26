using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhoneNumbers;

namespace Boilerplate.Tests.Services;

public partial class FakePhoneService(ServerApiAppSettings appSettings, IHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor, ILogger<PhoneService> logger, PhoneNumberUtil phoneNumberUtil)
    : PhoneService(appSettings, hostEnvironment, httpContextAccessor, logger, phoneNumberUtil)
{
    private static readonly ConcurrentDictionary<string, string> LastSmsPerPhone = new();

    public override Task SendSms(string messageText, string phoneNumber, CancellationToken cancellationToken)
    {
        LastSmsPerPhone.AddOrUpdate(phoneNumber, messageText, (_, _) => messageText);
        return Task.CompletedTask;
    }

    public static string GetLastSmsFor(string phoneNumber)
    {
        if (LastSmsPerPhone.TryGetValue(phoneNumber, out var message) is false)
            Assert.IsNotNull(message, "Sms has not sent");
        return message;
    }

    public static string GetLastOtpFor(string phoneNumber)
    {
        var message = GetLastSmsFor(phoneNumber);
        var otp = Regex.Match(message, @"\d{6}").Value;
        Assert.IsNotNull(otp, "Otp has not sent");
        return otp;
    }
}
