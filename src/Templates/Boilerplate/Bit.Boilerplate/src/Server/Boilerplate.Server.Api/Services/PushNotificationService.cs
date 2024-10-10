using AdsPush;
using AdsPush.Vapid;
using AdsPush.Abstraction;
using Boilerplate.Shared.Dtos.PushNotification;

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

        if (userId is not null)
        {
            tags.Add(userId.ToString()!);
        }

        var deviceInstallation = await dbContext.DeviceInstallations.FindAsync([dto.InstallationId], cancellationToken);

        if (deviceInstallation is null)
        {
            dbContext.DeviceInstallations.Add(deviceInstallation = new() { InstallationId = dto.InstallationId });
        }

        dto.Patch(deviceInstallation);

        deviceInstallation.UserId = userId;
        deviceInstallation.Tags = [.. tags];
        deviceInstallation.ExpirationTime = DateTimeOffset.UtcNow.AddMonths(1).DateTime;

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

    public async Task RequestPush(string? title = null, string? message = null, string? action = null, string[]? tags = null, CancellationToken cancellationToken = default)
    {
        tags ??= [];

        var sender = adsPushSenderFactory.GetSender("Primary");

        var devices = await dbContext.DeviceInstallations
            .WhereIf(tags.Any(), dev => dev.Tags.Any(t => tags.Contains(t)))
            .OrderByIf(tags.Any() is false, _ => EF.Functions.Random())
            .Take(100)
            .ToArrayAsync(cancellationToken);

        var payload = new AdsPushBasicSendPayload()
        {
            Title = AdsPushText.CreateUsingString(title),
            Detail = AdsPushText.CreateUsingString(message),
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
