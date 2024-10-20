using PhoneNumbers;
using Twilio.Rest.Api.V2010.Account;

namespace Boilerplate.Server.Api.Services;

public partial class PhoneService
{
    [AutoInject] private readonly ServerApiAppSettings appSettings = default!;
    [AutoInject] private readonly ILogger<PhoneService> logger = default!;
    [AutoInject] private readonly IHostEnvironment hostEnvironment = default!;
    [AutoInject] private readonly IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private readonly PhoneNumberUtil phoneNumberUtil = default!;
    private const string APP_DEFAULT_REGION = "US" /*Two letter ISO region name*/;

    public string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return null;

        // Get region from Cloudflare "CF-IPCountry" header if available, otherwise use UI culture's region if multilingual is enabled, or fallback to the default region.
        var region = httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("CF-IPCountry", out var value)
                         ? value.ToString()
                         : CultureInfoManager.MultilingualEnabled
                             ? new RegionInfo(CultureInfo.CurrentUICulture.Name).TwoLetterISORegionName
                             : APP_DEFAULT_REGION;

        var parsedPhoneNumber = phoneNumberUtil.Parse(phoneNumber, region);

        return phoneNumberUtil.Format(parsedPhoneNumber, PhoneNumberFormat.E164);
    }

    public async Task SendSms(string messageText, string phoneNumber, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment())
        {
            LogSendSms(logger, messageText, phoneNumber);
        }

        if (appSettings.Sms.Configured is false) return;

        var messageOptions = new CreateMessageOptions(new(phoneNumber))
        {
            From = new(appSettings.Sms.FromPhoneNumber),
            Body = messageText
        };

        var smsMessage = MessageResource.Create(messageOptions);

        if (smsMessage.ErrorCode is null) return;

        LogSendSmsFailed(logger, phoneNumber, smsMessage.ErrorCode, smsMessage.ErrorMessage);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS: {message} to {phoneNumber}.")]
    private static partial void LogSendSms(ILogger logger, string message, string phoneNumber);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send Sms to {phoneNumber}. Code: {errorCode}, Error message: {errorMessage}")]
    private static partial void LogSendSmsFailed(ILogger logger, string phoneNumber, int? errorCode, string errorMessage);
}
