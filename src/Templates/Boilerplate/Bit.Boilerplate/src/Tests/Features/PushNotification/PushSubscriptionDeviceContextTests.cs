//+:cnd:noEmit
using Boilerplate.Shared.Features.PushNotification;
using Boilerplate.Server.Api.Features.PushNotification;

namespace Boilerplate.Tests.Features.PushNotification;

/// <summary>
/// <c>IP</c>, <c>Address</c> and <c>AppVersionCode</c> live on the subscription row because an anonymous device has no
/// session to read them through, so they must be recorded without a session and rewritten on every subscribe.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class PushSubscriptionDeviceContextTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnonymousSubscribe_Should_RecordDeviceContext_AndRefreshItOnResubscribe()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        // A per-run device id, so leftovers are inert orphans rather than collisions.
        var deviceId = $"push-context-{Guid.NewGuid():N}";

        try
        {
            await Subscribe(country: "NL", city: "Amsterdam", appVersion: "1.2.3", pushChannel: "first-channel");

            var afterFirstSubscribe = await ReadSubscription(server, deviceId, TestContext.CancellationToken);

            Assert.IsNotNull(afterFirstSubscribe);
            Assert.IsNull(afterFirstSubscribe.UserSessionId, "The caller never signed in, so there is no session to carry any of this - which is the whole reason the columns are on the row.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(afterFirstSubscribe.IP), "The address the request came from must be recorded for an anonymous device too.");
            Assert.AreEqual("NL, Amsterdam", afterFirstSubscribe.Address, "Country and city come from the CDN's headers, formatted the same way UserSession formats them.");
            Assert.AreEqual(1_002_003, afterFirstSubscribe.AppVersionCode, "The version the device subscribed with is the X-App-Version every internal request already carries, encoded so it can be compared in SQL.");
            Assert.AreEqual("1.2.3", afterFirstSubscribe.AppVersion, "The stored code has to render back as the version the device reported.");

            // The same device, later, from somewhere else and after an app update.
            await Subscribe(country: "DE", city: "Berlin", appVersion: "1.3.0", pushChannel: "second-channel");

            var afterResubscribe = await ReadSubscription(server, deviceId, TestContext.CancellationToken);

            Assert.IsNotNull(afterResubscribe);
            Assert.AreEqual("DE, Berlin", afterResubscribe.Address, "Subscribe runs on every auth state change and app start, so these have to be rewritten alongside RenewedOn rather than kept from the first visit.");
            Assert.AreEqual(1_003_000, afterResubscribe.AppVersionCode, "A stale app version aims a version-targeted push at the wrong devices.");

            Assert.IsTrue(afterResubscribe.AppVersionCode > afterFirstSubscribe.AppVersionCode,
                "The whole point of the column: a later version has to sort above an earlier one, which the text form does not do ('1.10.0' < '1.9.0').");

            // Caller-supplied, so anything unrepresentable must land as null rather than collide with a real build.
            await Subscribe(country: "DE", city: "Berlin", appVersion: "not-a-version", pushChannel: "third-channel");

            var afterGarbage = await ReadSubscription(server, deviceId, TestContext.CancellationToken);

            Assert.IsNull(afterGarbage?.AppVersionCode, "An unparseable X-App-Version must not be stored as anything.");
            Assert.IsNull(afterGarbage?.AppVersion, "Nothing to decode means nothing to show.");
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PushNotificationSubscriptions.Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }

        // A raw client rather than the injected controller, since the headers under test are set by the client apps'
        // RequestHeadersDelegatingHandler and by the CDN, neither of which is in play here.
        async Task Subscribe(string country, string city, string appVersion, string pushChannel)
        {
            using var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            anonymousClient.DefaultRequestHeaders.Add("cf-ipcountry", country);
            anonymousClient.DefaultRequestHeaders.Add("cf-ipcity", city);
            anonymousClient.DefaultRequestHeaders.Add("X-App-Version", appVersion);

            var response = await anonymousClient.PostAsJsonAsync("api/v1/PushNotification/Subscribe",
                new PushNotificationSubscriptionDto { DeviceId = deviceId, Platform = "fcmV1", PushChannel = pushChannel },
                TestContext.CancellationToken);

            response.EnsureSuccessStatusCode();
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
