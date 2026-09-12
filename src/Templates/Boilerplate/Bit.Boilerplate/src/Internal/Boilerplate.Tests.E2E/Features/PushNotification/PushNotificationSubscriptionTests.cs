using System.Buffers.Text;
using System.Security.Cryptography;
using Boilerplate.Shared.Features.PushNotification;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.PushNotification;

/// <summary>
/// Push is the one feature whose whole delivery path is third party - VAPID and the browser's push service, FCM, APNS -
/// and none of it exists in process, so <c>Boilerplate.Tests</c> can only assert what is written down before the send:
/// the row, and the toggle recorded while the browser DENIES the permission.
/// <para>
/// What a deployment adds is everything up to the push service: whether it holds the keys at all, what the device
/// context on the row looks like once the request has been through Cloudflare, and whether asking for a push actually
/// puts a job on the queue and runs it. The delivery itself is the device's half, and belongs to
/// <c>AndroidPushNotificationTests</c> - here a subscription is deliberately addressed to nobody.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public class PushNotificationSubscriptionTests
{
    /// <summary>What DeployedApiClientProvider puts on every request, and so what the row has to encode.</summary>
    private const string appVersion = "400.0.0";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Nothing else notices a deployment that came back from a redeploy without its push keys: subscribing still
    /// answers 204, the row is still written, the job still runs, and no notification is ever delivered again.
    /// </summary>
    [TestMethod]
    public async Task TheDeployment_Should_HoldTheKeysEveryPlatformIsDeliveredWith()
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        var apiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);

        var info = await apiClient.McpClient!.GetDeploymentInfo(TestContext.CancellationToken);

        // DevMcpJson serializes with the web defaults, so the tool's payload is camelCase.
        var capabilities = info["capabilities"]
            ?? throw new AssertFailedException($"GetDeploymentInfo reported no capabilities at all: {info.ToJsonString()}");

        var webPushVapid = capabilities["webPushVapid"];
        var firebase = capabilities["firebase"];

        Assert.IsNotNull(webPushVapid, $"GetDeploymentInfo reports no webPushVapid capability, so this build has push compiled out: {capabilities.ToJsonString()}");
        Assert.IsNotNull(firebase, $"GetDeploymentInfo reports no firebase capability, so this build has push compiled out: {capabilities.ToJsonString()}");

        Assert.IsTrue(webPushVapid.GetValue<bool>(),
            "The deployment has no Web Push VAPID private key, so no browser or PWA subscription can be delivered to.");

        Assert.IsTrue(firebase.GetValue<bool>(),
            "The deployment has no Firebase private key, so no Android subscription can be delivered to.");
    }

    /// <summary>
    /// The row is what every later push is addressed by, and three of its columns are written from the request rather
    /// than from the body: the caller's IP as the deployment resolved it through Cloudflare's forwarded headers, the
    /// visitor location the edge adds, and the app version the client declared. An in-process test can only hand
    /// itself those headers; here they are the deployment's own answer.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    public async Task AnAnonymousSubscription_Should_RecordTheDeviceContextTheRequestArrivedWith(string api)
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        var cancellationToken = TestContext.CancellationToken;
        var subscription = NewBrowserSubscription();

        var publicIps = await PublicIpProvider.Resolve(cancellationToken);

        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(api);

        try
        {
            await apiClient.Services.GetRequiredService<IPushNotificationController>().Subscribe(subscription, cancellationToken);

            var stored = await ReadSubscription(subscription.DeviceId!);

            Assert.IsNotNull(stored, $"{api} answered the subscribe but wrote no row for the device.");

            Assert.IsNotNull(stored.IP, "The subscription was stored with no caller address at all.");
            Assert.Contains(PublicIpProvider.Normalize(stored.IP), publicIps,
                $"{api} recorded '{stored.IP}' as the subscribing device's address. Behind Cloudflare that is the edge's own address, which is the forwarded-headers configuration not being applied - and every subscription then looks like it came from the same device.");

            Assert.AreEqual(AppVersionCodes.TryEncode(appVersion), stored.AppVersionCode,
                "The X-App-Version the request carried is not what the row encodes, so a push aimed at outdated devices would miss this one.");

            // Written from cf-ipcountry / cf-ipcity, which only a deployment with Cloudflare's visitor location
            // transform in front of it has - the standalone APIs are reached directly, so the value is theirs to be
            // empty. What must hold everywhere is that the column was written rather than left null.
            Assert.IsNotNull(stored.Address, "The subscription was stored with no address column at all.");
            TestContext.WriteLine($"{api} recorded the visitor location as '{stored.Address}'.");

            Assert.IsGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), stored.ExpirationTime,
                "The subscription was stored already expired, so RequestPush would never pick it up.");
        }
        finally
        {
            await Unsubscribe(api, subscription);
        }

        Assert.IsNull(await ReadSubscription(subscription.DeviceId!),
            $"{api} kept the row after the device unsubscribed, so it would keep being pushed to.");
    }

    /// <summary>
    /// <c>TestPushNotificationSetup</c> is the welcome push a device gets the moment it subscribes, and it is the
    /// shortest path through everything the server owns: the subscription is found, a job is enqueued for it, and
    /// Hangfire runs that job. Only the hand-off to the push service is beyond this - the endpoint is deliberately
    /// given a subscription addressed to nobody, so no real device is disturbed by a test run.
    /// </summary>
    [TestMethod]
    public async Task AskingForTheWelcomePush_Should_EnqueueAndRunAJobForThatDeviceAlone()
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        var cancellationToken = TestContext.CancellationToken;
        var subscription = NewBrowserSubscription();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken);

        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.AdminPanelApi);
        var pushNotificationController = apiClient.Services.GetRequiredService<IPushNotificationController>();

        try
        {
            await pushNotificationController.Subscribe(subscription, cancellationToken);

            var stored = await ReadSubscription(subscription.DeviceId!);
            Assert.IsNotNull(stored, "The subscribe wrote no row, so there is nothing for the welcome push to be addressed to.");

            // By the row's own id: the job's arguments carry the subscription ids it was given, so this both waits for
            // the right job and proves the filter picked this device and not the hundred others on the deployment.
            var marker = stored.Id.ToString();
            var before = await globalApiClient.McpClient!.HangfireJobIds(marker, cancellationToken);

            await pushNotificationController.TestPushNotificationSetup(subscription, cancellationToken);

            var job = await globalApiClient.McpClient.WaitForHangfireJob(marker, before, cancellationToken);

            Assert.Contains(nameof(Server.Api.Features.PushNotification.PushNotificationJobRunner), job.ToJsonString(), StringComparison.OrdinalIgnoreCase,
                $"The job enqueued for this subscription is not the push runner: {job}");
        }
        finally
        {
            await Unsubscribe(DeployedApps.AdminPanelApi, subscription);
        }
    }

    /// <summary>
    /// A subscription the server will accept and nothing will ever be delivered to: <c>browser</c> takes the Web Push
    /// triple, and the endpoint's keys are read by AdsPush rather than stored as given, so the public key has to be a
    /// real P-256 point. The endpoint host is FCM's, which is where a Chrome subscription's is.
    /// </summary>
    private static PushNotificationSubscriptionDto NewBrowserSubscription()
    {
        using var keyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = keyPair.PublicKey.ExportParameters().Q;

        var marker = Guid.NewGuid().ToString("N")[..10];

        return new()
        {
            DeviceId = $"e2e-{marker}",
            Platform = "browser",
            Endpoint = $"https://fcm.googleapis.com/fcm/send/e2e-{marker}",
            P256dh = Base64Url.EncodeToString([0x04, .. publicKey.X!, .. publicKey.Y!]),
            Auth = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16))
        };
    }

    private async Task<Server.Api.Features.PushNotification.PushNotificationSubscription?> ReadSubscription(string deviceId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        return await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(sub => sub.DeviceId == deviceId, TestContext.CancellationToken);
    }

    /// <summary>
    /// Through the app's own endpoint, then the row directly: not on the test's token, because a canceled or timed out
    /// test still owes the deployment a database without its fixtures in it.
    /// </summary>
    private static async Task Unsubscribe(string api, PushNotificationSubscriptionDto subscription)
    {
        try
        {
            await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(api);
            await apiClient.Services.GetRequiredService<IPushNotificationController>().Unsubscribe(subscription, CancellationToken.None);
        }
        catch (Exception)
        {
            // Never stored, or already gone - the row delete below is what has to hold either way.
        }

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters()
            .Where(sub => sub.DeviceId == subscription.DeviceId)
            .ExecuteDeleteAsync(CancellationToken.None);
    }
}
