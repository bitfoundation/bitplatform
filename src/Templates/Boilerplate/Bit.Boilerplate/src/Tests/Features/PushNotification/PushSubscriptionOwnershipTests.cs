//+:cnd:noEmit
using Boilerplate.Shared.Features.PushNotification;
using Boilerplate.Server.Api.Features.PushNotification;

namespace Boilerplate.Tests.Features.PushNotification;

/// <summary>
/// <c>POST /api/v1/PushNotification/Subscribe</c> is <c>[AllowAnonymous]</c> by design - an anonymous visitor's
/// own device has no <c>UserSession</c> to authenticate with. What it must NOT do is let an anonymous caller
/// mutate a subscription row that already belongs to somebody's session, because the row carries the push
/// endpoint the server sends OTP, two-factor and reset-password notifications to. Detaching it from its session
/// silently stops every one of those, with no error anywhere.
/// <para>
/// This ships in the DEFAULT configuration (<c>notification == true</c>), which is why it is worth an
/// integration test rather than a note.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class PushSubscriptionOwnershipTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnonymousSubscribe_Should_NotTakeOverAnotherSessionsDevice()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        // A per-run device id, so anything this test leaves behind is an inert orphan rather than a collision with
        // the shared development database.
        var deviceId = $"push-ownership-{Guid.NewGuid():N}";
        const string legitimatePushChannel = "legitimate-push-channel";

        try
        {
            await scope.ServiceProvider.GetRequiredService<IPushNotificationController>()
                .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = legitimatePushChannel }, TestContext.CancellationToken);

            var ownedSubscription = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNotNull(ownedSubscription, "The authenticated subscribe should have created the row this test is about.");
            Assert.IsNotNull(ownedSubscription.UserSessionId, "A subscribe made by a signed-in caller must bind the row to that caller's session, otherwise the rest of this test proves nothing.");

            var sessionIdBefore = ownedSubscription.UserSessionId;

            // A raw HttpClient rather than the DI one or the typed proxy on purpose: both attach the caller's
            // bearer token through AuthDelegatingHandler, and the whole point here is a request with NO identity.
            using var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            var takeoverAttempt = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "attacker-push-channel" },
                TestContext.CancellationToken);

            takeoverAttempt.EnsureSuccessStatusCode(); // The endpoint is anonymous, so this legitimately succeeds - it just must not touch the owned row.

            var afterTakeoverAttempt = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNotNull(afterTakeoverAttempt, "The owned row must still exist.");

            Assert.AreEqual(sessionIdBefore, afterTakeoverAttempt.UserSessionId,
                "An anonymous caller supplying a known DeviceId detached the row from its owner's session. Every UserRelatedPush filters on `s.UserSession.UserId == user.Id`, so this silently stops that device receiving OTP, two-factor and reset-password notifications.");

            Assert.AreEqual(legitimatePushChannel, afterTakeoverAttempt.PushChannel,
                "An anonymous caller repointed an owned subscription's push channel, which aims the app's own sender at a device of the caller's choosing.");

            await using (var countScope = server.WebApp.Services.CreateAsyncScope())
            {
                var dbContext = countScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var rowCount = await dbContext.PushNotificationSubscriptions.CountAsync(s => s.DeviceId == deviceId, TestContext.CancellationToken);

                Assert.AreEqual(1, rowCount,
                    "Refusing the takeover must not fall through to an INSERT either: that would turn the same unauthenticated request into a way to grow the table one row at a time.");
            }
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PushNotificationSubscriptions.Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }

    /// <summary>
    /// The anonymous path still has to work for the device it belongs to: the web client calls Subscribe on every
    /// auth-state propagation, including the anonymous one that runs before anybody signs in, and after sign-out.
    /// A fix that scoped the lookup too tightly would leave a duplicate row per sign-out instead.
    /// </summary>
    [TestMethod]
    public async Task AnonymousSubscribe_Should_StillRenewItsOwnUnownedDevice()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var deviceId = $"push-anon-{Guid.NewGuid():N}";

        try
        {
            using var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var response = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                    new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = $"channel-{attempt}" },
                    TestContext.CancellationToken);

                response.EnsureSuccessStatusCode();
            }

            await using var scope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var rows = await dbContext.PushNotificationSubscriptions.Where(s => s.DeviceId == deviceId).ToArrayAsync(TestContext.CancellationToken);

            Assert.HasCount(1, rows, "Re-subscribing the same anonymous device must renew its row, not accumulate a new one on every call.");
            Assert.AreEqual("channel-2", rows[0].PushChannel, "The renewal must actually update the push channel.");
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PushNotificationSubscriptions.Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }

    /// <summary>
    /// "One row per device" is not a convention here, it is what <c>Subscribe</c>'s lookup, the ownership refusal
    /// above and <c>DiagnosticController.PerformDiagnostic</c>'s by-device lookup all assume. Asserted against the
    /// EF model rather than by inserting a colliding row, so it is provider accurate and writes nothing.
    /// </summary>
    [TestMethod]
    public async Task DeviceId_Should_BeUnique()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entityType = dbContext.Model.FindEntityType(typeof(PushNotificationSubscription))!;

        var deviceIdIndex = entityType.GetIndexes()
                                      .SingleOrDefault(i => i.Properties.Count is 1 && i.Properties[0].Name == nameof(PushNotificationSubscription.DeviceId));

        Assert.IsNotNull(deviceIdIndex,
            $"PushNotificationSubscription.DeviceId has no index. Indexes present: [{string.Join(", ", entityType.GetIndexes().Select(i => string.Join("+", i.Properties.Select(p => p.Name))))}].");

        Assert.IsTrue(deviceIdIndex.IsUnique,
            "Without uniqueness the same device can end up with several subscription rows, and every by-device lookup then picks an arbitrary one.");
    }

    private static async Task<PushNotificationSubscription?> ReadSubscription(AppTestServer server, string deviceId, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.PushNotificationSubscriptions.AsNoTracking()
                                                            .FirstOrDefaultAsync(s => s.DeviceId == deviceId, cancellationToken);
    }
}
