//+:cnd:noEmit
using Microsoft.Extensions.Time.Testing;
using Boilerplate.Shared.Features.PushNotification;
using Boilerplate.Server.Api.Features.PushNotification;

namespace Boilerplate.Tests.Features.PushNotification;

/// <summary>
/// <c>POST /api/v1/PushNotification/Subscribe</c> is <c>[AllowAnonymous]</c> by design - an anonymous visitor's own
/// device has no <c>UserSession</c> to authenticate with - and it deliberately performs no ownership check: the
/// <c>DeviceId</c> is the device's credential, so whoever presents one gets that device's row. The reasoning, and why
/// an ownership check breaks more than it protects, is written out at the check's absence in
/// <c>PushNotificationService.Subscribe</c>.
/// <para>
/// What this file pins is the invariant that replaces it: <b>one row per device, one row per session, and the device's
/// row always follows whoever is using the device.</b> Every test here drives a flow that an ownership check would have
/// broken - clearing tokens without signing out, signing in again, handing the device to somebody else - because those
/// are the flows that were found to break, not hypotheticals.
/// </para>
/// <para>
/// This ships in the DEFAULT configuration (<c>notification == true</c>), which is why it is worth an integration test
/// rather than a note.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class PushSubscriptionOwnershipTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// A device is shared, or handed over, or simply used by somebody else next. When it is, its push subscription
    /// belongs to whoever is signed in on it now - the previous user's row has to be taken over rather than defended.
    /// <para>
    /// The previous user's <c>UserSession</c> is deliberately left alive here (nobody signs out), because that is the
    /// state an ownership check would trip over: sign out is not guaranteed to happen, and until
    /// <c>UserSessionsRetentionJobRunner</c> removes it the row still points at a session nobody is using.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task AnotherUserOnTheSameDevice_Should_TakeOverItsSubscription()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        // A per-run device id, so anything this test leaves behind is an inert orphan rather than a collision with
        // the shared development database.
        var deviceId = $"push-ownership-{Guid.NewGuid():N}";

        try
        {
            Guid firstSessionId;

            await using (var firstUserScope = server.WebApp.Services.CreateAsyncScope())
            {
                await TestAccountUtils.CreateAndSignIn(server, firstUserScope, TestContext.CancellationToken);

                await firstUserScope.ServiceProvider.GetRequiredService<IPushNotificationController>()
                    .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "first-user-channel" }, TestContext.CancellationToken);
            }

            var afterFirstUser = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNotNull(afterFirstUser?.UserSessionId, "A subscribe made by a signed-in caller must bind the row to that caller's session, otherwise the rest of this test proves nothing.");
            firstSessionId = afterFirstUser.UserSessionId!.Value;

            // A different account entirely, on the same device. IStorageService is registered per scope, so this scope
            // carries its own bearer token.
            await using (var secondUserScope = server.WebApp.Services.CreateAsyncScope())
            {
                await TestAccountUtils.CreateAndSignIn(server, secondUserScope, TestContext.CancellationToken);

                await secondUserScope.ServiceProvider.GetRequiredService<IPushNotificationController>()
                    .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "second-user-channel" }, TestContext.CancellationToken);
            }

            var afterSecondUser = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNotNull(afterSecondUser?.UserSessionId);

            Assert.AreNotEqual(firstSessionId, afterSecondUser.UserSessionId,
                "The device's subscription must follow whoever is signed in on it now. Refusing this is what an ownership check would do, and it would leave the new user permanently without push on their own device.");

            Assert.AreEqual("second-user-channel", afterSecondUser.PushChannel,
                "Taking the row over must update the push channel too, otherwise the app keeps sending to a channel the device no longer listens on.");

            await using (var countScope = server.WebApp.Services.CreateAsyncScope())
            {
                var dbContext = countScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var rowCount = await dbContext.PushNotificationSubscriptions.CountAsync(s => s.DeviceId == deviceId, TestContext.CancellationToken);

                Assert.AreEqual(1, rowCount,
                    "One device, one row: DeviceId is unique, so a hand-over must re-point the existing row rather than insert a second one.");
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
    /// The flow that shows why <c>Subscribe</c> performs no ownership check at all: sign in, subscribe, drop the tokens
    /// from local storage / the cookie without signing out, then sign in again on the same device.
    /// <para>
    /// Clearing the tokens tells the server nothing, so the previous <c>UserSession</c> row is still there and the
    /// device's subscription is still pointing at it - for up to <c>Identity:RefreshTokenExpiration</c>, until
    /// <c>UserSessionsRetentionJobRunner</c> removes it. Both the anonymous propagation that runs first and the
    /// authenticated one that follows therefore present a DeviceId whose row belongs to a session neither of them owns.
    /// Any ownership check refuses both and leaves that device without push for good, which is why the rule is simply
    /// that whoever presents the DeviceId gets the row (See <c>PushNotificationService.Subscribe</c>).
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task SigningInAgainOnTheSameDevice_Should_RebindTheSubscriptionToTheNewSession()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var deviceId = $"push-resignin-{Guid.NewGuid():N}";

        try
        {
            string email;
            Guid firstSessionId;

            await using (var firstScope = server.WebApp.Services.CreateAsyncScope())
            {
                (email, _) = await TestAccountUtils.CreateAndSignIn(server, firstScope, TestContext.CancellationToken);

                await firstScope.ServiceProvider.GetRequiredService<IPushNotificationController>()
                    .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "channel-before" }, TestContext.CancellationToken);
            }

            var afterFirstSignIn = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNotNull(afterFirstSignIn?.UserSessionId, "The first subscribe should have bound the row to the first session.");
            firstSessionId = afterFirstSignIn.UserSessionId!.Value;

            // Step one of the real sequence, and the one that fails first: the tokens are gone, so the app reloads
            // ANONYMOUS and AppClientCoordinator propagates that state before anything else - which calls Subscribe
            // with no identity at all, for a device whose row is still bound to the surviving first session. A raw
            // HttpClient because the DI one and the typed proxy both attach a bearer token through
            // AuthDelegatingHandler, and the whole point here is a request carrying none.
            using (var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress })
            {
                var anonymousPropagation = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                    new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "channel-anonymous" },
                    TestContext.CancellationToken);

                anonymousPropagation.EnsureSuccessStatusCode();
            }

            var afterAnonymousPropagation = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNull(afterAnonymousPropagation?.UserSessionId,
                "The device is no longer signed in, so its own anonymous re-subscribe has to detach the row from the session whose tokens are gone. Refusing it - which an ownership check does - leaves the row pointing at a dead session and the device without push.");

            // A brand-new scope is a brand-new (empty) token store - exactly what clearing local storage and the cookie
            // leaves behind. Nothing signs the first session out, so its UserSession row survives on the server.
            await using (var secondScope = server.WebApp.Services.CreateAsyncScope())
            {
                var identityController = secondScope.ServiceProvider.GetRequiredService<IIdentityController>();

                await identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken);

                var captured = await server.WaitForCapturedEmail(email,
                    capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);

                var tokens = await identityController.SignIn(new() { Email = email, Otp = captured.Token }, TestContext.CancellationToken);
                await secondScope.ServiceProvider.GetRequiredService<AuthManager>().StoreTokens(tokens);

                // This is the call the shipped client makes on the very first auth-state propagation after signing in.
                await secondScope.ServiceProvider.GetRequiredService<IPushNotificationController>()
                    .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "channel-after" }, TestContext.CancellationToken);
            }

            var afterSecondSignIn = await ReadSubscription(server, deviceId, TestContext.CancellationToken);

            Assert.IsNotNull(afterSecondSignIn?.UserSessionId);
            Assert.AreNotEqual(firstSessionId, afterSecondSignIn.UserSessionId,
                "Signing in again on the same device must re-point that device's subscription at the new session, otherwise push keeps being addressed to a session whose tokens the user no longer has.");
            Assert.AreEqual("channel-after", afterSecondSignIn.PushChannel, "The re-subscribe must update the push channel too.");

            await using var countScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = countScope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.AreEqual(1, await dbContext.PushNotificationSubscriptions.CountAsync(s => s.DeviceId == deviceId, TestContext.CancellationToken),
                "One device, one row - re-signing in must not accumulate a second one.");
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PushNotificationSubscriptions.Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }

    /// <summary>
    /// <c>PushChannel</c> is <c>[Required]</c> on the entity (a NOT NULL column) but optional on the DTO, and browsers
    /// legitimately send the web push triple instead of a channel. So the platform decides which field is mandatory, and
    /// the check has to live on the server: without it a body missing its own platform's field reaches SaveChanges and
    /// comes back as a 500 logged at Critical - from an endpoint that is anonymous and, per BP-160, unthrottled. The
    /// iOS / MacCatalyst clients produce exactly this body when the APNs token never arrives (they log and carry on).
    /// </summary>
    [TestMethod]
    public async Task Subscribe_Should_RejectASubscriptionMissingItsPushChannel()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var pushNotificationController = scope.ServiceProvider.GetRequiredService<IPushNotificationController>();

        // A device token platform with no token at all.
        await Assert.ThrowsExactlyAsync<BadRequestException>(() => pushNotificationController.Subscribe(
            new() { DeviceId = $"push-nochannel-{Guid.NewGuid():N}", Platform = "apns" }, TestContext.CancellationToken));

        // The browser derives its channel from Endpoint + P256dh + Auth, and VapidSubscription.FromParameters accepts
        // nulls without complaining, so a partial triple would otherwise be stored as an undeliverable subscription.
        await Assert.ThrowsExactlyAsync<BadRequestException>(() => pushNotificationController.Subscribe(
            new() { DeviceId = $"push-nochannel-{Guid.NewGuid():N}", Platform = "browser", Endpoint = "https://push.example/endpoint" }, TestContext.CancellationToken));
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

    /// <summary>
    /// <c>Subscribe</c> runs on every auth-state propagation, i.e. on every page refresh, and the overwhelmingly common
    /// call changes nothing at all - yet <c>RenewedOn</c> and <c>ExpirationTime</c> are assigned unconditionally, so
    /// every one of those calls issues an UPDATE. Every visitor, every refresh, forever.
    /// <para>
    /// <b>Ignored on purpose.</b> The renewal throttle this pins was written and then removed by the maintainer
    /// (2026-08-05, BP-252), so the test currently fails - and it is kept rather than deleted because it is the
    /// executable form of the open finding. <b>What would unblock it:</b> deciding that an unchanged Subscribe need not
    /// renew the window, i.e. letting the change tracker gate the write
    /// (<c>if (dbContext.Entry(subscription).State is not EntityState.Unchanged || RenewedOn is older than N)</c>).
    /// With no modified property left, <c>SaveChangesAsync</c> issues no command at all. Both windows this feeds are
    /// measured in days - <c>RenewedOn</c> against <c>Identity:RefreshTokenExpiration</c> (14) and
    /// <c>ExpirationTime</c> a month out - so an hour of staleness moves nothing. Verified failing for exactly that
    /// reason, and passing with the throttle in place, before it was ignored.
    /// </para>
    /// <para>
    /// It doubles as a guard on everything else the method assigns: <c>Tags</c> is rebuilt from the current culture on
    /// every call and <c>dto.Patch</c> rewrites every mapped member, so if any of them ever started registering as
    /// modified when the values are identical (an EF value-comparer change on the <c>string[]</c> primitive collection
    /// would do it), that would show up here too.
    /// </para>
    /// </summary>
    [TestMethod, Ignore("BP-252 is open: the renewal throttle was removed by the maintainer, so an unchanged Subscribe still writes a row on every page refresh. Un-ignore when the write is gated on the change tracker - see the remarks above.")]
    public async Task RepeatingAnIdenticalSubscribe_Should_NotWriteToTheDatabase()
    {
        // RenewedOn is unix SECONDS, and two back-to-back calls land in the same second on the wall clock - so left on
        // it, this test would pass whether or not the throttle exists. The fake clock is moved forward between calls by
        // less than the renewal interval, which is the only way "it did not renew" means anything. It is seeded from
        // now so bearer-token validation stays free of skew.
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(fakeTimeProvider));
        }).Start(TestContext.CancellationToken);

        var deviceId = $"push-renewal-{Guid.NewGuid():N}";

        try
        {
            using var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            async Task Subscribe(string pushChannel)
            {
                var response = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                    new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = pushChannel },
                    TestContext.CancellationToken);

                response.EnsureSuccessStatusCode();
            }

            await Subscribe("channel-1");
            var afterFirst = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNotNull(afterFirst);

            // Far enough for a wall-clock write to be visible in unix seconds, well inside MinimumRenewalInterval.
            fakeTimeProvider.Advance(TimeSpan.FromMinutes(10));

            await Subscribe("channel-1");
            var afterIdenticalCall = await ReadSubscription(server, deviceId, TestContext.CancellationToken);

            Assert.AreEqual(afterFirst.RenewedOn, afterIdenticalCall!.RenewedOn,
                "A Subscribe call that changes nothing must not renew the window either, otherwise every page refresh of every visitor writes a row.");

            // Non-vacuity: a call that DOES change something has to renew in the same write, or a device whose push
            // channel rotates would sit with a stale window until it expires.
            fakeTimeProvider.Advance(TimeSpan.FromMinutes(10));

            await Subscribe("channel-2");
            var afterRealChange = await ReadSubscription(server, deviceId, TestContext.CancellationToken);

            Assert.AreEqual("channel-2", afterRealChange!.PushChannel, "The renewal must still update a changed push channel.");
            Assert.IsGreaterThan(afterFirst.RenewedOn, afterRealChange.RenewedOn,
                "A Subscribe call that changed the push channel must renew the window in the same write.");
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PushNotificationSubscriptions.Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }

    /// <summary>
    /// The only case where two rows match at once: this session already holds one device's row, and the device it now
    /// reports already has a row of its own. <c>UserSessionId</c> is unique, so the session has to be released from the
    /// first row before it can be written onto the second - and in a separate <c>SaveChanges</c>, because EF gives no
    /// ordering guarantee between two independent UPDATEs in one batch and an acquire-before-release ordering violates
    /// the index. Rare, which is exactly why it is worth pinning: it would surface as an intermittent 500 on an
    /// anonymous endpoint.
    /// </summary>
    [TestMethod]
    public async Task ASessionMovingToADeviceThatAlreadyHasARow_Should_ReleaseItsPreviousOne()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var firstDeviceId = $"push-two-rows-a-{Guid.NewGuid():N}";
        var secondDeviceId = $"push-two-rows-b-{Guid.NewGuid():N}";

        try
        {
            // An unowned row for the second device, the way an anonymous visit leaves one behind.
            using (var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress })
            {
                var response = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                    new PushNotificationSubscriptionDto { DeviceId = secondDeviceId, Platform = "fcmV1", PushChannel = "anonymous-channel" },
                    TestContext.CancellationToken);

                response.EnsureSuccessStatusCode();
            }

            await using var scope = server.WebApp.Services.CreateAsyncScope();

            await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

            var pushNotificationController = scope.ServiceProvider.GetRequiredService<IPushNotificationController>();

            // This session takes the first device...
            await pushNotificationController.Subscribe(
                new() { DeviceId = firstDeviceId, Platform = "fcmV1", PushChannel = "first-channel" }, TestContext.CancellationToken);

            var firstRow = await ReadSubscription(server, firstDeviceId, TestContext.CancellationToken);
            Assert.IsNotNull(firstRow?.UserSessionId, "The session has to own the first device's row, otherwise only one row matches below and the case under test never arises.");

            // ...and now reports the second, whose row already exists. Both rows match the lookup.
            await pushNotificationController.Subscribe(
                new() { DeviceId = secondDeviceId, Platform = "fcmV1", PushChannel = "second-channel" }, TestContext.CancellationToken);

            var secondRow = await ReadSubscription(server, secondDeviceId, TestContext.CancellationToken);
            var firstRowAfterwards = await ReadSubscription(server, firstDeviceId, TestContext.CancellationToken);

            Assert.IsNotNull(secondRow);
            Assert.AreEqual(firstRow.UserSessionId, secondRow.UserSessionId,
                "The device the session now reports must end up owning it.");

            Assert.IsNull(firstRowAfterwards?.UserSessionId,
                "The row the session held before must have let go of it. UserSessionId is unique, so leaving both set is a constraint violation rather than a stale row.");

            Assert.AreEqual("second-channel", secondRow!.PushChannel, "The second device's channel must be the one that got written.");
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PushNotificationSubscriptions
                .Where(s => s.DeviceId == firstDeviceId || s.DeviceId == secondDeviceId)
                .ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }

    /// <summary>
    /// The user-facing half of the AppMenu push notifications toggle: turning it off calls <c>Unsubscribe</c>, which
    /// removes the device's row so the server stops addressing it - and it has to work for an anonymous caller, because
    /// the toggle is offered whether the user is signed in or not. Same "DeviceId is the credential" model as
    /// <c>Subscribe</c>: no ownership check, idempotent for a device that has no row, and turning the toggle back on
    /// simply subscribes again.
    /// </summary>
    [TestMethod]
    public async Task AnonymousUnsubscribe_Should_RemoveTheDeviceRow_AndStayIdempotent()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var deviceId = $"push-unsub-{Guid.NewGuid():N}";

        try
        {
            using var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            // A JSON body rather than a bare route value, because AutoCsrfProtectionFilter only lets an anonymous
            // (no Authorization header) unsafe request through when it is JSON - which is also why the shipped
            // endpoint takes the dto.
            async Task<HttpResponseMessage> Unsubscribe() =>
                await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Unsubscribe",
                    new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "channel-off" },
                    TestContext.CancellationToken);

            // Unsubscribing a device that never subscribed is what a fresh install's toggle-off produces; it must not fail.
            (await Unsubscribe()).EnsureSuccessStatusCode();

            var subscribeResponse = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "channel-on" },
                TestContext.CancellationToken);
            subscribeResponse.EnsureSuccessStatusCode();

            Assert.IsNotNull(await ReadSubscription(server, deviceId, TestContext.CancellationToken),
                "The subscribe that precedes the toggle-off must have stored a row, otherwise the removal below proves nothing.");

            (await Unsubscribe()).EnsureSuccessStatusCode();

            Assert.IsNull(await ReadSubscription(server, deviceId, TestContext.CancellationToken),
                "Turning push notifications off must remove the device's subscription row; a surviving row keeps receiving pushes the user just declined.");

            // Toggling back on is a plain re-subscribe and must work after the row was deleted.
            var resubscribeResponse = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "channel-on-again" },
                TestContext.CancellationToken);
            resubscribeResponse.EnsureSuccessStatusCode();

            var afterResubscribe = await ReadSubscription(server, deviceId, TestContext.CancellationToken);
            Assert.IsNotNull(afterResubscribe);
            Assert.AreEqual("channel-on-again", afterResubscribe.PushChannel, "Toggling back on must store a fresh, deliverable subscription.");
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PushNotificationSubscriptions.Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }

    private static async Task<PushNotificationSubscription?> ReadSubscription(AppTestServer server, string deviceId, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.PushNotificationSubscriptions.AsNoTracking()
                                                            .FirstOrDefaultAsync(s => s.DeviceId == deviceId, cancellationToken);
    }
}
