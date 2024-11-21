using AdsPush;
using AdsPush.Vapid;
using AdsPush.Abstraction;
using System.Linq.Expressions;
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Server.Api.Models.PushNotification;

namespace Boilerplate.Server.Api.Services;

public partial class PushNotificationService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IAdsPushSender adsPushSender = default!;
    [AutoInject] private ILogger<PushNotificationService> logger = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;

    public async Task RegisterDevice([Required] DeviceInstallationDto dto, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        var userSessionId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetSessionId() : (Guid?)null;

        var deviceInstallation = await dbContext.DeviceInstallations.FindAsync([dto.InstallationId], cancellationToken);

        if (deviceInstallation is null)
        {
            dbContext.DeviceInstallations.Add(deviceInstallation = new()
            {
                InstallationId = dto.InstallationId,
                Platform = dto.Platform
            });
        }

        dto.Patch(deviceInstallation);

        deviceInstallation.Tags = [.. tags];
        deviceInstallation.UserSessionId = userSessionId;
        deviceInstallation.RenewedOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        deviceInstallation.ExpirationTime = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds();

        if (deviceInstallation.Platform is "browser")
        {
            deviceInstallation.PushChannel = VapidSubscription.FromParameters(deviceInstallation.Endpoint, deviceInstallation.P256dh, deviceInstallation.Auth).ToAdsPushToken();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeregisterDevice(string deviceInstallationId, CancellationToken cancellationToken)
    {
        dbContext.DeviceInstallations.Remove(new() { InstallationId = deviceInstallationId });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestPush(string? title = null, string? message = null, string? action = null,
        bool userRelatedPush = false,
        Expression<Func<DeviceInstallation, bool>>? customDeviceFilter = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // userRelatedPush: If the BearerTokenExpiration is 14 days, it’s not practical to send push notifications to a device where the user hasn't used the app for over 14 days.  
        // This is because, even if the user opens the app, they will be automatically signed out as their session has expired.  
        // Therefore, sending push notifications with sensitive information, like an OTP code, to such devices is not appropriate.

        var query = dbContext.DeviceInstallations
            .Where(dev => dev.ExpirationTime > now)
            .WhereIf(customDeviceFilter is not null, customDeviceFilter!)
            .WhereIf(userRelatedPush is true, dev => (now - dev.RenewedOn) < serverApiSettings.Identity.BearerTokenExpiration.TotalSeconds);

        if (customDeviceFilter is null)
        {
            query = query.OrderBy(_ => EF.Functions.Random()).Take(100);
        }

        var devices = await query.ToListAsync(cancellationToken);

        var payload = new AdsPushBasicSendPayload()
        {
            Title = AdsPushText.CreateUsingString(title ?? "Boilerplate"),
            Detail = AdsPushText.CreateUsingString(message ?? string.Empty),
            Parameters = new Dictionary<string, object>()
            {
                {
                    "action", action ?? string.Empty
                }
            }
        };

        List<Task> tasks = [];

        foreach (var deviceInstallation in devices)
        {
            var target = deviceInstallation.Platform is "browser" ? AdsPushTarget.BrowserAndPwa
                : deviceInstallation.Platform is "fcmV1" ? AdsPushTarget.Android
                : deviceInstallation.Platform is "apns" ? AdsPushTarget.Ios
                : throw new NotImplementedException();

            tasks.Add(adsPushSender.BasicSendAsync(target, deviceInstallation.PushChannel, payload, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to send push notification.");
        }
    }
}
