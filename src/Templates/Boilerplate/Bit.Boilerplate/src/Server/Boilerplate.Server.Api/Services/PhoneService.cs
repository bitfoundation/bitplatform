using PhoneNumbers;
using Twilio.Rest.Api.V2010.Account;

namespace Boilerplate.Server.Api.Services;

public partial class PhoneService
{
    [AutoInject] private readonly ServerApiSettings appSettings = default!;
    [AutoInject] private readonly PhoneNumberUtil phoneNumberUtil = default!;
    [AutoInject] private readonly IHostEnvironment hostEnvironment = default!;
    [AutoInject] private readonly ILogger<PhoneService> phoneLogger = default!;
    [AutoInject] private readonly IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private readonly RootServiceScopeProvider rootServiceScopeProvider = default!;

    public virtual string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return null;

        // Get region from Cloudflare "CF-IPCountry" header if available, otherwise use UI culture's region if multilingual is enabled, or fallback to the default region.
        var region = httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("CF-IPCountry", out var value)
                         ? value.ToString()
                         : new RegionInfo((CultureInfoManager.MultilingualEnabled ? CultureInfo.CurrentUICulture : CultureInfoManager.DefaultCulture).Name).TwoLetterISORegionName;

        var parsedPhoneNumber = phoneNumberUtil.Parse(phoneNumber, region);

        return phoneNumberUtil.Format(parsedPhoneNumber, PhoneNumberFormat.E164);
    }

    public virtual async Task SendSms(string messageText, string phoneNumber, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment())
        {
            LogSendSms(phoneLogger, messageText, phoneNumber);
        }

        if (appSettings.Sms?.Configured is false) return;

        var from = appSettings.Sms!.FromPhoneNumber;

        _ = Task.Run(async () => // Let's not wait for the sms to be sent. Consider using a proper message queue or background job system like Hangfire.
        {
            await using var scope = rootServiceScopeProvider.Invoke();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<PhoneService>>();
            MessageResource? smsMessage = null;
            try
            {
                var messageOptions = new CreateMessageOptions(new(phoneNumber))
                {
                    From = new(from),
                    Body = messageText
                };

                smsMessage = MessageResource.Create(messageOptions);

                if (smsMessage.ErrorCode is not null)
                    throw new InvalidOperationException(smsMessage.ErrorMessage);
            }
            catch (Exception exp)
            {
                LogSendSmsFailed(logger, exp, phoneNumber, smsMessage?.ErrorCode);
            }
        }, default);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS: {message} to {phoneNumber}.")]
    private static partial void LogSendSms(ILogger logger, string message, string phoneNumber);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send Sms to {phoneNumber}. Code: {errorCode}")]
    private static partial void LogSendSmsFailed(ILogger logger, Exception exp, string phoneNumber, int? errorCode);
}
