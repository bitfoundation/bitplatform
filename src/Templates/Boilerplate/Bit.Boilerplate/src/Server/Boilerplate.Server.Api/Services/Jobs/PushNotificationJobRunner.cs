using AdsPush;
using AdsPush.Abstraction;
using System.Collections.Concurrent;

namespace Boilerplate.Server.Api.Services.Jobs;

public partial class PushNotificationJobRunner
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IAdsPushSender adsPushSender = default!;
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;

    public async Task RequestPush(int[] pushNotificationSubscriptionIds,
        string? title = null,
        string? message = null,
        string? action = null,
        string? pageUrl = null,
        bool userRelatedPush = false,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await dbContext.PushNotificationSubscriptions
            .Where(pns => pushNotificationSubscriptionIds.Contains(pns.Id))
            .ToArrayAsync(cancellationToken);

        var payload = new AdsPushBasicSendPayload()
        {
            Title = AdsPushText.CreateUsingString(title ?? "Boilerplate push"),
            Detail = AdsPushText.CreateUsingString(message ?? string.Empty)
        };

        if (string.IsNullOrEmpty(action) is false)
        {
            payload.Parameters.Add("action", action);
        }
        if (string.IsNullOrEmpty(pageUrl) is false)
        {
            payload.Parameters.Add("pageUrl", pageUrl);
        }

        ConcurrentBag<Exception> exceptions = [];

        await Parallel.ForEachAsync(subscriptions, parallelOptions: new()
        {
            MaxDegreeOfParallelism = 10,
            CancellationToken = default
        }, async (subscription, _) =>
        {
            try
            {
                var target = subscription.Platform is "browser" ? AdsPushTarget.BrowserAndPwa
                                    : subscription.Platform is "fcmV1" ? AdsPushTarget.Android
                                    : subscription.Platform is "apns" ? AdsPushTarget.Ios
                                    : throw new NotImplementedException();

                await adsPushSender.BasicSendAsync(target, subscription.PushChannel, payload, default);
            }
            catch (Exception exp)
            {
                exceptions.Add(exp);
            }
        });

        if (exceptions.IsEmpty is false)
        {
            serverExceptionHandler.Handle(new AggregateException("Failed to send push notifications", exceptions)
                .WithData(new() { { "UserRelatedPush", userRelatedPush } }));
        }
    }
}
