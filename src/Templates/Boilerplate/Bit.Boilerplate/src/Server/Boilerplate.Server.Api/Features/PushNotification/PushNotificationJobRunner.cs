//+:cnd:noEmit
using AdsPush;
using AdsPush.Abstraction;
using System.Collections.Concurrent;
using Hangfire.Server;
using System.Net;
//#if (signalR == true)
using Boilerplate.Shared.Infrastructure.Dtos.SignalR;
//#endif

namespace Boilerplate.Server.Api.Features.PushNotification;

public partial class PushNotificationJobRunner
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IAdsPushSender adsPushSender = default!;
    [AutoInject] private ApiServerExceptionHandler serverExceptionHandler = default!;
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
            .Include(pns => pns.UserSession!.User)
            .ToArrayAsync(cancellationToken);

        string?[] devicesWithSeveralAccounts = [];

        if (request.UserRelatedPush)
        {
            var deviceIds = subscriptions.Select(pns => pns.DeviceId).ToArray();

            devicesWithSeveralAccounts = await dbContext.PushNotificationSubscriptions
                .Where(pns => deviceIds.Contains(pns.DeviceId) && pns.UserSessionId != null)
                .GroupBy(pns => pns.DeviceId)
                .Where(device => device.Count() > 1)
                .Select(device => device.Key)
                .ToArrayAsync(cancellationToken);
        }

        AdsPushBasicSendPayload CreatePayload(User? account)
        {
            var title = request.Title ?? "Boilerplate push";
            var pageUrl = request.PageUrl;

            if (account is not null)
            {
                title = $"{title} ({account.Email ?? account.PhoneNumber ?? account.UserName})";

                if (string.IsNullOrWhiteSpace(pageUrl) is false)
                {
                    pageUrl = $"{pageUrl}{(pageUrl.Contains('?') ? '&' : '?')}{DeviceAccountDto.QueryParameter}={account.Id}";
                }
            }

            var payload = new AdsPushBasicSendPayload()
            {
                Title = AdsPushText.CreateUsingString(title),
                Detail = AdsPushText.CreateUsingString(request.Message ?? string.Empty)
            };

            if (string.IsNullOrWhiteSpace(request.Action) is false)
            {
                payload.Parameters.Add("action", request.Action);
            }
            if (string.IsNullOrWhiteSpace(pageUrl) is false)
            {
                payload.Parameters.Add("pageUrl", pageUrl);
            }

            return payload;
        }

        int succeededItems = 0;
        int failedItems = 0;
        //#if (signalR == true)
        string? signalRConnectionId = null;
        if (request.RequesterUserSessionId != null) // Instead of passing SignalRConnectionId directly, we get it from UserSessionId to have latest value at the time of job execution
        {
            signalRConnectionId = await dbContext.UserSessions
                .Where(us => us.Id == request.RequesterUserSessionId)
                .Select(us => us.SignalRConnectionId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        //#endif

        ConcurrentBag<(int problematicSubscriptionId, AdsPushErrorType? errorType, HttpStatusCode? responseStatusCode, Exception exp)> problems = [];

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
                                    : throw new NotImplementedException($"Platform {subscription.Platform} is not supported.");

                var payload = CreatePayload(devicesWithSeveralAccounts.Contains(subscription.DeviceId) ? subscription.UserSession?.User : null);

                await adsPushSender.BasicSendAsync(target, subscription.PushChannel, payload, default);

                Interlocked.Increment(ref succeededItems); // Inside Parallel.ForEachAsync simple ++ wouldn't work
            }
            catch (Exception exp)
            {
                Interlocked.Increment(ref failedItems);
                var adsPushException = exp as AdsPushException;
                problems.Add((subscription.Id, adsPushException?.ErrorType, adsPushException?.HttpResponse?.StatusCode, exp));
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

        if (problems.IsEmpty is false)
        {
            var errorData = new Dictionary<string, object?>()
            {
                { "UserRelatedPush", request.UserRelatedPush },
                { "JobId", context.BackgroundJob.Id },
                { "TotalSubscriptions", pushNotificationSubscriptionIds.Length },
                { "SucceededItems", succeededItems },
                { "FailedItems", failedItems }
            };

            foreach (var (problematicSubscriptionId, errorType, responseStatusCode, exp) in problems.DistinctBy(p => new { p.errorType, p.responseStatusCode, ExceptionType = p.exp.GetType() }))
            {
                errorData[$"Subscription_{problematicSubscriptionId}"] = $"ErrorType: {errorType}, ResponseStatusCode: {responseStatusCode}, Exception: {exp.GetType().Name}: {exp.Message}";
            }

            serverExceptionHandler.Handle(new AggregateException("Failed to send push notifications", problems.Select(p => p.exp)).WithData(errorData));
        }
    }
}
