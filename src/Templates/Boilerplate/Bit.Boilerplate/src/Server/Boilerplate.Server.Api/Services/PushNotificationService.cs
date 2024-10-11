using AdsPush;
using AdsPush.Vapid;
using AdsPush.Abstraction;
using System.Linq.Expressions;
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Server.Api.Models.PushNotification;

namespace Boilerplate.Server.Api.Services;

public partial class PushNotificationService
{
    [AutoInject] private IAdsPushSenderFactory adsPushSenderFactory = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private AppDbContext dbContext = default!;

    public async Task RegisterDevice([Required] DeviceInstallationDto dto, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        var userId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetUserId() : (Guid?)null;

        var deviceInstallation = await dbContext.DeviceInstallations.FindAsync([dto.InstallationId], cancellationToken);

        if (deviceInstallation is null)
        {
            dbContext.DeviceInstallations.Add(deviceInstallation = new() { InstallationId = dto.InstallationId });
        }

        dto.Patch(deviceInstallation);

        deviceInstallation.UserId = userId;
        deviceInstallation.Tags = [.. tags];
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

    public async Task RequestPush(string? title = null, string? message = null, string? action = null, Expression<Func<DeviceInstallation, bool>>? customDeviceFilter = null, CancellationToken cancellationToken = default)
    {
        var sender = adsPushSenderFactory.GetSender("Primary");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var query = dbContext.DeviceInstallations
            .Where(dev => dev.ExpirationTime > now)
            .WhereIf(customDeviceFilter is not null, customDeviceFilter!);

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

            tasks.Add(sender.BasicSendAsync(target, deviceInstallation.PushChannel, payload, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}
