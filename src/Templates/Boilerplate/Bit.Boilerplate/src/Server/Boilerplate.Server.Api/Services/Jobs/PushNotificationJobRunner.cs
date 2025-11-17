//+:cnd:noEmit
using AdsPush;
using AdsPush.Abstraction;
using System.Collections.Concurrent;
using Hangfire.Server;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
using Boilerplate.Shared.Dtos.SignalR;
//#endif

namespace Boilerplate.Server.Api.Services.Jobs;

public partial class PushNotificationJobRunner
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IAdsPushSender adsPushSender = default!;
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> hubContext = default!;
    //#endif

    public async Task RequestPush(int[] pushNotificationSubscriptionIds,
        PushNotificationRequest request,
        PerformContext context = null!,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await dbContext.PushNotificationSubscriptions
            .Where(pns => pushNotificationSubscriptionIds.Contains(pns.Id))
            .ToArrayAsync(cancellationToken);

        var payload = new AdsPushBasicSendPayload()
        {
            Title = AdsPushText.CreateUsingString(request.Title ?? "Boilerplate push"),
            Detail = AdsPushText.CreateUsingString(request.Message ?? string.Empty)
        };

        if (string.IsNullOrEmpty(request.Action) is false)
        {
            payload.Parameters.Add("action", request.Action);
        }
        if (string.IsNullOrEmpty(request.PageUrl) is false)
        {
            payload.Parameters.Add("pageUrl", request.PageUrl);
        }

        //#if (signalR == true)
        int failedItems = 0;
        int succeededItems = 0;
        string? signalRConnectionId = null;
        if (request.RequesterUserSessionId != null) // Instead of passing SignalRConnectionId directly, we get it from UserSessionId to have latest value at the time of job execution
        {
            signalRConnectionId = await dbContext.UserSessions
                .Where(us => us.Id == request.RequesterUserSessionId)
                .Select(us => us.SignalRConnectionId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        //#endif

        ConcurrentBag<Exception> exceptions = [];
        ConcurrentBag<int> problematicSubscriptionIds = [];

        await Parallel.ForEachAsync(subscriptions, parallelOptions: new()
        {
            MaxDegreeOfParallelism = 10,
            CancellationToken = cancellationToken
        }, async (subscription, cancellationToken) =>
        {
            try
            {
                var target = subscription.Platform is "browser" ? AdsPushTarget.BrowserAndPwa
                                    : subscription.Platform is "fcmV1" ? AdsPushTarget.Android
                                    : subscription.Platform is "apns" ? AdsPushTarget.Ios
                                    : throw new NotImplementedException();

                await adsPushSender.BasicSendAsync(target, subscription.PushChannel, payload, default);

                //#if (signalR == true)
                Interlocked.Increment(ref succeededItems); // Inside Parallel.ForEachAsync simple ++ wouldn't work
                //#endif
            }
            catch (Exception exp)
            {
                //#if (signalR == true)
                Interlocked.Increment(ref failedItems);
                //#endif
                exceptions.Add(exp);
                problematicSubscriptionIds.Add(subscription.Id);
            }
            //#if (signalR == true)
            finally
            {
                try
                {
                    if (signalRConnectionId != null)
                    {
                        _ = hubContext.Clients.Client(signalRConnectionId).Publish(SharedAppMessages.BACKGROUND_JOB_PROGRESS, new BackgroundJobProgressDto()
                        {
                            JobId = context.BackgroundJob.Id,
                            JobTitle = nameof(AppStrings.PushNotificationJob),
                            TotalItems = pushNotificationSubscriptionIds.Length,
                            SucceededItems = succeededItems,
                            FailedItems = failedItems
                        }, cancellationToken);
                    }
                }
                catch { }
            }
            //#endif
        });

        if (exceptions.IsEmpty is false)
        {
            serverExceptionHandler.Handle(new AggregateException("Failed to send push notifications", exceptions)
                .WithData(new()
                {
                    { "UserRelatedPush", request.UserRelatedPush },
                    { "JobId", context.BackgroundJob.Id  },
                    { "ProblematicSubscriptionIds", problematicSubscriptionIds }
                }));
        }
    }
}
