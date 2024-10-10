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
            .OrderByIf(tags.Any() is false, _ => Guid.NewGuid())
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

        var tasks = new List<Task>();

        foreach (var deviceInstallation in devices)
        {
            switch (deviceInstallation.Platform)
            {
                case "browser":
                    {
                        var subscription = VapidSubscription.FromParameters(deviceInstallation.Endpoint, deviceInstallation.P256dh, deviceInstallation.Auth);
                        tasks.Add(sender.BasicSendAsync(AdsPushTarget.BrowserAndPwa, subscription.ToAdsPushToken(), payload, cancellationToken));
                        break;
                    }
                case "fcmV1":
                    {
                        break;
                    }
                case "apns":
                    {
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        await Task.WhenAll(tasks);
    }
}
