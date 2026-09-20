//+:cnd:noEmit
using Boilerplate.Shared.Features.PushNotification;
using Boilerplate.Server.Api.Features.PushNotification;

namespace Boilerplate.Tests.Features.PushNotification;

/// <summary>
/// A subscription row keeps a device identifier and the Web Push keys its payloads are encrypted with. <c>RequestPush</c>
/// stops sending to one past its <c>ExpirationTime</c>, but nothing used to delete it, so the identifiers accumulated
/// for as long as the database lived.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class PushSubscriptionsRetentionTests
{
    [TestMethod]
    public async Task EnforceRetention_Should_DeleteExpiredSubscriptions_AndKeepLiveOnes()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        // Per-run device ids, so anything left behind is an inert orphan rather than a collision with the shared
        // development database.
        var expiredDeviceId = $"push-retention-expired-{Guid.NewGuid():N}";
        var liveDeviceId = $"push-retention-live-{Guid.NewGuid():N}";

        try
        {
            await Subscribe(server, expiredDeviceId);
            await Subscribe(server, liveDeviceId);

            await Expire(server, expiredDeviceId);

            await EnforceRetention(server);

            Assert.IsFalse(await Exists(server, expiredDeviceId),
                "A subscription nothing can send to any more must not keep the device's identifier and push keys on file.");

            Assert.IsTrue(await Exists(server, liveDeviceId),
                "A live subscription must survive the sweep, or every device silently stops receiving notifications.");
        }
        finally
        {
            await Delete(server, liveDeviceId);
        }
    }

    private async Task Subscribe(AppTestServer server, string deviceId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IPushNotificationController>()
            .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = $"channel-{deviceId}" }, TestContext.CancellationToken);
    }

    /// <summary>Backdated rather than waited out: Subscribe stamps an expiry a month ahead.</summary>
    private async Task Expire(AppTestServer server, string deviceId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var expiredOn = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().AddMinutes(-1).ToUnixTimeSeconds();

        await scope.ServiceProvider.GetRequiredService<AppDbContext>().PushNotificationSubscriptions
            .Where(sub => sub.DeviceId == deviceId)
            .ExecuteUpdateAsync(sub => sub.SetProperty(x => x.ExpirationTime, expiredOn), TestContext.CancellationToken);
    }

    private async Task EnforceRetention(AppTestServer server)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<PushSubscriptionsRetentionJobRunner>()
            .EnforceRetention(TestContext.CancellationToken);
    }

    private async Task<bool> Exists(AppTestServer server, string deviceId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().PushNotificationSubscriptions
            .AnyAsync(sub => sub.DeviceId == deviceId, TestContext.CancellationToken);
    }

    private async Task Delete(AppTestServer server, string deviceId)
    {
        try
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<AppDbContext>().PushNotificationSubscriptions
                .Where(sub => sub.DeviceId == deviceId)
                .ExecuteDeleteAsync(TestContext.CancellationToken);
        }
        catch (Exception) { }
    }

    public TestContext TestContext { get; set; } = default!;
}
