using PhoneNumbers;

namespace Boilerplate.Server.Api.Infrastructure.Services;

public partial class PhoneService
{
    [AutoInject] private readonly ServerApiSettings appSettings = default!;
    [AutoInject] private readonly IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private readonly PhoneNumberUtil phoneNumberUtil = default!;
    [AutoInject] private readonly IHostEnvironment hostEnvironment = default!;
    [AutoInject] private readonly ILogger<PhoneService> phoneLogger = default!;
    [AutoInject] private readonly IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private readonly IBackgroundJobClient backgroundJobClient = default!;

    public virtual string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return null;

        // Get region from Cloudflare "CF-IPCountry" header if available, otherwise use UI culture's region if multilingual is enabled, or fallback to the default region.
        var region = httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("CF-IPCountry", out var value)
                         ? value.ToString()
                         : new RegionInfo((CultureInfoManager.InvariantGlobalization is false ? CultureInfo.CurrentUICulture : CultureInfoManager.DefaultCulture).Name).TwoLetterISORegionName;

        try
        {
            var parsedPhoneNumber = phoneNumberUtil.Parse(phoneNumber, region);

            return phoneNumberUtil.Format(parsedPhoneNumber, PhoneNumberFormat.E164);
        }
        catch (NumberParseException exp)
        {
            // libphonenumber is far stricter than the [Phone] data annotation the DTOs carry, so a value that passed
            // model validation can still land here - "+1" does. The region is caller-influenced too: Cloudflare sends
            // XX for unknown geography and T1 for Tor, and neither is a region libphonenumber accepts, which makes any
            // national-format number throw for a perfectly legitimate visitor. Unhandled this surfaces as a 500 logged
            // at Critical on an anonymous endpoint; it is a bad request.
            throw new BadRequestException(localizer[nameof(AppStrings.PhoneAttribute_ValidationError), localizer[nameof(AppStrings.PhoneNumber)]])
                .WithData("Reason", exp.Message)
                .WithData("Region", region);
        }
    }

    public virtual async Task SendSms(string messageText, string phoneNumber)
    {
        if (hostEnvironment.IsDevelopment())
        {
            LogSendSms(phoneLogger, messageText, phoneNumber);
        }

        if (appSettings.Sms?.Configured is false) return;

        var from = appSettings.Sms!.FromPhoneNumber!;

        backgroundJobClient.Enqueue<PhoneServiceJobsRunner>(x => x.SendSms(phoneNumber, from, messageText));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS: {message} to {phoneNumber}.")]
    private static partial void LogSendSms(ILogger logger, string message, string phoneNumber);
}
